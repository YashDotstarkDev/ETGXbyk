using AngleSharp.Dom;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.GoogleReviews;
using ETG.Core.Models.Tour;
using ETG.Core.Modules;
using ETG.Web.Features.TourPackage;
using ETG.Web.Infrastructure;
using ETG.Web.Services.GoogleReviews;
using ETG.Web.Services.Tour;
using ETGBooking;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "ETG.TourPostPageTemplates",
    name: "Tour Page - Default",
    propertiesType: typeof(TourPageTemplateProperties),
    customViewName: "~/Features/TourPackage/TourPostPage_Default.cshtml",
    ContentTypeNames = [Tour.CONTENT_TYPE_NAME],
    Description = "",
    IconClass = ""
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Tour.CONTENT_TYPE_NAME,
    controllerType: typeof(TourPageTemplateController),
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.TourPackage;

public sealed class TourPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class TourPageTemplateController(
IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IContentQueryExecutor executor,
    WebPageMetaService metaService,
    IShareLinksService shareLinksService,
    ITourSummaryService tourSummaryService,
    ITourPricingService pricingService,
    IHttpContextAccessor httpContextAccessor,
    IContentRetriever contentRetriever,
    IInfoProvider<PromotionInfo> promotionProvider,
    IWebPageDataContextRetriever contextRetriever,
    IGoogleReviewsService reviewsService,
     IWebsiteChannelContext channelContext,
    IInfoProvider<TourTypeInfo> tourTypeProvider) : Controller
{
    private readonly IMediator mediator = mediator;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly WebPageMetaService metaService = metaService;
    private readonly IContentQueryExecutor executor = executor;
    private readonly ITourSummaryService tourSummaryService = tourSummaryService;
    private readonly ITourPricingService pricingService = pricingService;
    private readonly IShareLinksService shareLinksService = shareLinksService;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IInfoProvider<TourTypeInfo> tourTypeProvider = tourTypeProvider;
    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IGoogleReviewsService reviewsService = reviewsService;
    private readonly IInfoProvider<PromotionInfo> promotionProvider = promotionProvider;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    [HttpGet("~/enquire/{tourCode}")]
    public async Task<IActionResult> Enquire(string tourCode, string? date = null, string? flightclass = "Economy", string? city = null)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return NotFound();
        }
              
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Tour.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite("ETG")
                    .WithLinkedItems(4)
                    .Where(where =>
                        where.WhereEquals(nameof(Tour.TourCode), tourCode)));

        var tours = await executor.GetMappedWebPageResult<Tour>(query);
        var tour = tours.FirstOrDefault();

        if (tour == null)
        {
            return NotFound();
        }

        ViewData["BreadCrumbTour"] = tour;
        var offerData = await GetTourOfferData(tour);
        var discountText = offerData.Discount != null
            ? $"{offerData.Discount.DiscountHeading} - Expires {offerData.Discount.DiscountExpiry:dd/MM/yyyy}"
            : string.Empty;
        var priceInfo = await pricingService.GetPriceAsync(tour);
        var tourQuotedPrice = priceInfo.CurrentPrice > 0 ? priceInfo.CurrentPrice.ToString() : string.Empty;
        var priceCurrency = priceInfo.CurrencyCode;
        var shareLinks = shareLinksService.GetShareLinks();
        var destinations = await GetDestinations(tour.TourPrimaryCountry);
        var tourdestination = destinations.FirstOrDefault(d => d.SystemFields.WebPageItemGUID == tour.TourPrimaryCountry?.FirstOrDefault()?.WebPageGuid);
        var viewModel = new TourEnquireViewModel
        {
            Tour = tour,
            ShareLinks = shareLinks,
            Form = new TourEnquireFormViewModel
            {
                TourCode = tour.TourCode,
                TourName = tour.TourName,
                TourLink = tour.SystemFields.WebPageUrlPath ?? string.Empty,
                TourDepartureCity = !string.IsNullOrWhiteSpace(city) ? city : tour.TourDepartureCity,
                TourClass = flightclass ?? string.Empty,
                TourQuotedPrice = tourQuotedPrice,
                TourDate = tour.TourTravelDates ?? string.Empty,
                DepartureDate = DateTime.Parse(date ?? DateTime.Now.ToString()),
                ReturnDate = DateTime.Parse(date ?? DateTime.Now.ToString()),
                TourDiscountText = discountText,
                PriceCurrency = priceCurrency ?? string.Empty,
                PreferredDestination = tourdestination?.DestinationName ?? string.Empty

            }
        };

        return View("~/Features/TourPackage/TourEnquire.cshtml", viewModel);
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var tour = await mediator.Send(new Operations.TourPostPageQuery(data.WebPage));

        if (tour is null)
        {
            return NotFound();
        }

        ViewData["BreadCrumbTour"] = tour;

        // PERFORMANCE FIX: Use cached CQRS query handler
        var faqs = await mediator.Send(
            new Operations.GetTourFaqsQuery(
                channelContext.WebsiteChannelName,
                tour.TourFAQFolder),
            HttpContext.RequestAborted);

        var destinations = await GetDestinations(tour.TourPrimaryCountry);
        // PERFORMANCE FIX: Use cached CQRS query handler with optimized date filtering
        //var promotion = await mediator.Send(
        //    new Operations.GetActiveTourPromotionQuery(tour.TourCode),
        //    HttpContext.RequestAborted);

        var offerData = await GetTourOfferData(tour);

        var priceInfo = await pricingService.GetPriceAsync(tour);

        var tourTypeNames = new List<string>();

        var tourTypeCodeNames = tour.TourTypes?
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Select(x => x.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

        if (tourTypeCodeNames.Length > 0)
        {
            tourTypeNames = (await tourTypeProvider
                .Get()
                .WhereIn(
                    nameof(TourTypeInfo.TourTypeCodeName),
                    tourTypeCodeNames)
                .GetEnumerableTypedResultAsync())
                .Select(x => x.TourTypeName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        // PERFORMANCE FIX: Use cached CQRS query handler
        var experienceGuids = tour.TourExperiences?
            .Where(x => x?.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid)
            .ToArray() ?? Array.Empty<Guid>();

        var experiences = await mediator.Send(
            new Operations.GetExperiencesQuery(
                channelContext.WebsiteChannelName,
                experienceGuids),
            HttpContext.RequestAborted);

        var partnershipFolders = new List<Folder>();

        if (tour.TourInPartnershipPath != null &&
            tour.TourInPartnershipPath.Any())
        {
            var partnershipPageGuids = tour.TourInPartnershipPath
                .Where(x => x != null && x.WebPageGuid != Guid.Empty)
                .Select(x => x.WebPageGuid)
                .ToList();

            if (partnershipPageGuids.Any())
            {
                var partnershipQuery = new ContentItemQueryBuilder()
                    .ForContentType(
                        Folder.CONTENT_TYPE_NAME,
                        config =>
                        {
                            config.WithLinkedItems(4);
                            config.Where(w =>
                                w.WhereIn(
                                    "ContentItemGUID",
                                    partnershipPageGuids
                                )
                            );
                        });

                var folders =
                    await executor.GetMappedWebPageResult<Folder>(
                        partnershipQuery,
                        cancellationToken: CancellationToken.None
                    );

                partnershipFolders = folders.ToList();
            }
        }


        var relatedTours = await mediator.Send(
    new Operations.GetRelatedToursQuery(tour),
    HttpContext.RequestAborted);

        var mappedTours = new List<TourSummaryViewModel>();

        if (relatedTours != null && relatedTours.Count > 0)
        {
            var validRelatedTours = relatedTours
                .Where(relatedTour =>
                    relatedTour != null &&
                    !string.IsNullOrWhiteSpace(
                        relatedTour.TourCode))
                .Take(8)
                .ToList();

            if (validRelatedTours.Count > 0)
            {
                // =========================================================
                // 1. COLLECT PRIMARY COUNTRY GUIDS
                // =========================================================

                var relatedCountryGuids = validRelatedTours
                    .Select(relatedTour =>
                        relatedTour.TourPrimaryCountry?
                            .FirstOrDefault(item =>
                                item != null &&
                                item.WebPageGuid != Guid.Empty)?
                            .WebPageGuid
                        ?? Guid.Empty)
                    .Where(guid => guid != Guid.Empty)
                    .Distinct()
                    .ToArray();


                // =========================================================
                // 2. START ALL INDEPENDENT OPERATIONS TOGETHER
                // =========================================================

                var relatedPricesTask =
                    pricingService.GetPricesBulkAsync(
                        validRelatedTours);

                var destinationsTask =
                    GetDestinationsLookup(
                        relatedCountryGuids);

                var urlTasks = validRelatedTours
                    .Select(async relatedTour =>
                    {
                        var url =
                            await urlRetriever.Retrieve(
                                relatedTour);

                        return new
                        {
                            Tour = relatedTour,
                            Path = url?.RelativePath
                                ?? string.Empty
                        };
                    })
                    .ToArray();


                // =========================================================
                // 3. WAIT FOR ALL OPERATIONS
                // =========================================================

                await Task.WhenAll(
                    relatedPricesTask,
                    destinationsTask);


                var relatedTourPrices =
                    await relatedPricesTask;

                var destinationLookup =
                    await destinationsTask;

                var relatedTourUrls =
                    await Task.WhenAll(urlTasks);


                // =========================================================
                // 4. MAP RELATED TOURS
                // =========================================================

                foreach (var item in relatedTourUrls)
                {
                    // -----------------------------------------------------
                    // Primary country GUID
                    // -----------------------------------------------------

                    var countryGuid =
                        item.Tour.TourPrimaryCountry?
                            .FirstOrDefault(country =>
                                country != null &&
                                country.WebPageGuid != Guid.Empty)?
                            .WebPageGuid
                        ?? Guid.Empty;


                    // -----------------------------------------------------
                    // Primary country name
                    // -----------------------------------------------------

                    string? primaryCountryName = null;

                    if (countryGuid != Guid.Empty &&
                        destinationLookup.TryGetValue(
                            countryGuid,
                            out var destination))
                    {
                        primaryCountryName =
                            destination.DestinationName;
                    }


                    // -----------------------------------------------------
                    // Bulk pricing
                    // -----------------------------------------------------

                    relatedTourPrices.TryGetValue(
                        item.Tour.TourCode,
                        out var relatedPriceInfo);


                    // -----------------------------------------------------
                    // Build ViewModel
                    // -----------------------------------------------------

                    var tourViewModel =
                        await tourSummaryService.MapToViewModelAsync(
                            item.Tour,
                            item.Path,
                            relatedPriceInfo,
                            primaryCountryName);

                    mappedTours.Add(tourViewModel);
                }
            }
        }

        var featureTours = new TourListingViewModel
        {
            Heading = "SOMEWHERE ELSE TO CONSIDER",
            SectionId = "related-tours",
            Tours = mappedTours,
            ViewAllUrl = "/search",
            HideViewAllButton = false
        };

        var shareLinks = shareLinksService.GetShareLinks();
        var reviews = await reviewsService.GetReviewsAsync();
        var request = httpContextAccessor.HttpContext?.Request;

        var currentUrl =
            $"{request.Scheme}://{request.Host}" +
            $"{request.PathBase}" +
            $"{request.Path}" +
            $"{request.QueryString}";
        var encodedUrl =
           Uri.EscapeDataString(currentUrl);
        var ctaimage = tour?.TourHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        var ctaurl = new ContactUsViewModel
        {
            CTAUrl = !string.IsNullOrWhiteSpace(tour?.TourCode)
                ? $"/enquire/{tour.TourCode}"
                : string.Empty,

            CTABackgroundImage = ctaimage?.LegacyMediaFileAsset?.Url
                                 ?? "/images/travel_destination.png"
        };
        var FreedomOfChoiceDays = GetFreedomOfChoiceDays(tour);
        var TourUpgrades = await GetTourUpgradesAsync(tour);
        var viewModel = new TourPageTemplateViewModel
        {
            Tour = tour,
            CTA = ctaurl,
            PriceInfo = priceInfo,
            FeatureTours = featureTours,
            Experiences = experiences,
            Destinations = destinations,
            PartnershipFolders = partnershipFolders,
            ShareLinks = shareLinks,
            CurrentUrl = encodedUrl,
            TourTypeNames = tourTypeNames,
            Reviews = reviews,
            FAQs = faqs,
            Promotion = offerData.Promotion,
            Discount = offerData.Discount,
            FreedomOfChoiceDays = FreedomOfChoiceDays,
            TourUpgrades = TourUpgrades
        };

        if (tour is IMetaDataSchema metaFields)
        {
            var heroImage = tour.TourHeroImage?
                .OfType<ETG.Core.Content.MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/TourPackage/TourPostPage_Default.cshtml",
            viewModel);
        //return new TemplateResult(viewModel);
    }
    private async Task<IEnumerable<ETG.Core.Content.Experience>> GetExperiences(
    IEnumerable<WebPageRelatedItem> selectedItems)
    {
        var selectedGuids = selectedItems?
            .Where(x => x != null && x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid)
            .Distinct()
            .ToArray() ?? [];

        if (selectedGuids.Length == 0)
        {
            return [];
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                ETG.Core.Content.Experience.CONTENT_TYPE_NAME,
                config => config.ForWebsite("ETG"));

        var pages = await executor.GetMappedWebPageResult<ETG.Core.Content.Experience>(
            query,
            cancellationToken: HttpContext.RequestAborted);

        var pagesByGuid = pages.ToDictionary(
            x => x.SystemFields.WebPageItemGUID);

        return selectedGuids
            .Where(guid => pagesByGuid.ContainsKey(guid))
            .Select(guid => pagesByGuid[guid])
            .ToList();
    }

    private async Task<IEnumerable<Destination>> GetDestinations(
    IEnumerable<WebPageRelatedItem> selectedItems)
    {
        var selectedGuids = selectedItems?
            .Where(x => x != null && x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid)
            .Distinct()
            .ToArray() ?? [];

        if (selectedGuids.Length == 0)
        {
            return [];
        }

        var destinations = await contentRetriever.RetrievePagesByGuids<Destination>(selectedGuids);

        return destinations.ToList();
    }

    private async Task<TourOfferData> GetTourOfferData(Tour tour)
    {
        if (tour == null)
        {
            return new TourOfferData();
        }

        // PERFORMANCE FIX: Use cached CQRS query handler
        var promotion = await mediator.Send(
            new Operations.GetActiveTourPromotionQuery(tour.TourCode),
            HttpContext.RequestAborted);

        if (promotion != null)
        {
            return new TourOfferData
            {
                Promotion = promotion,
                Discount = null
            };
        }

        var discount = GetActiveTourDiscount(tour.TourDiscountAndOffer);

        return new TourOfferData
        {
            Promotion = null,
            Discount = discount
        };
    }

    private async Task<PromotionInfo?> GetActiveTourPromotion(string tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var promotions = await promotionProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        return promotions
            .Where(promotion =>
                !string.IsNullOrWhiteSpace(promotion.PromotionPackageCodes))
            .Where(promotion =>
                promotion.PromotionPackageCodes
                    .Split(
                        new[] { "\r\n", "\n", "\r" },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Any(x =>
                        string.Equals(
                            x,
                            tourCode,
                            StringComparison.OrdinalIgnoreCase)))
            .Where(promotion =>
                promotion.PromotionFromDate != default &&
                promotion.PromotionToDate != default &&
                now >= promotion.PromotionFromDate &&
                now <= promotion.PromotionToDate)
            .OrderByDescending(promotion => promotion.PromotionFromDate)
            .FirstOrDefault();
    }

    private static ETG.DiscountAndOffer? GetActiveTourDiscount(
    IEnumerable<ETG.DiscountAndOfferFolder>? folders)
    {
        if (folders == null)
        {
            return null;
        }

        var today = DateTime.UtcNow.Date;

        foreach (var folder in folders)
        {
            if (folder == null)
            {
                continue;
            }

            var discount = folder.SelectedDiscountAndOffers?
                .FirstOrDefault(item =>
                    item != null &&
                    item.DiscountIsOnSaleNow &&
                    item.DiscountExpiry.Date >= today);

            if (discount != null)
            {
                return discount;
            }

            var nestedDiscount = GetActiveTourDiscount(
                folder.SelectedDiscountAndOfferFolders);

            if (nestedDiscount != null)
            {
                return nestedDiscount;
            }
        }

        return null;
    }

    private async Task<PromotionInfo?> GetTourPromotions(string tourCode)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            return null;
        }

        var promotions = await promotionProvider
            .Get()
            .GetEnumerableTypedResultAsync();

        foreach (var promotion in promotions)
        {
            if (string.IsNullOrWhiteSpace(promotion.PromotionPackageCodes))
            {
                continue;
            }

            var packageCodes = promotion.PromotionPackageCodes
                .Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());
            foreach (var packageCode in packageCodes)
            {
                if (packageCodes.Any(x => string.Equals(x, tourCode, StringComparison.OrdinalIgnoreCase)))
                {
                    return promotion;
                }
            }
        }

        return null;
    }

    private async Task<List<FAQ>> GetTourFaqs(Tour tour, CancellationToken cancellationToken)
    {

        if (tour.TourFAQFolder != null && tour.TourFAQFolder.Any())
        {
            var selectedFaqs = tour.TourFAQFolder
                .Where(folder => folder != null)
                .SelectMany(folder =>
                    folder.SelectedFAQs ?? Enumerable.Empty<FAQ>())
                .Where(faq => faq != null)
                .ToList();

            return selectedFaqs;
        }

        var defaultFolder = await GetDefaultFaqFolder();

        if (defaultFolder == null)
        {
            return [];
        }

        return (defaultFolder.SelectedFAQs ??
                Enumerable.Empty<FAQ>())
            .Where(faq => faq != null)
            .ToList();
    }

    private async Task<Dictionary<Guid, Destination>>
    GetDestinationsLookup(IEnumerable<Guid> destinationGuids)
    {
        var guids = destinationGuids
            .Where(guid => guid != Guid.Empty)
            .Distinct()
            .ToArray();

        if (guids.Length == 0)
        {
            return new Dictionary<Guid, Destination>();
        }

        var destinations =
            await contentRetriever.RetrievePagesByGuids<Destination>(
                guids);

        return destinations
            .Where(destination =>
                destination?.SystemFields != null &&
                destination.SystemFields.WebPageItemGUID != Guid.Empty)
            .GroupBy(destination =>
                destination.SystemFields.WebPageItemGUID)
            .ToDictionary(
                group => group.Key,
                group => group.First());
    }
    private async Task<Folder?> GetDefaultFaqFolder()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Folder.CONTENT_TYPE_NAME,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(
                        "ETG",
                        pathMatch: PathMatch.Single("/Shared/FAQs")));

        var folders = await executor.GetMappedWebPageResult<Folder>(
            query,
            cancellationToken: HttpContext.RequestAborted);

        return folders.FirstOrDefault();
    }

    private static List<string> GetFreedomOfChoiceDays(Tour tour)
    {
        return tour?.SelectedItineraryFolders?
            .Where(folder => folder != null)
            .SelectMany(folder => folder.SelectedTourItineraries ?? Enumerable.Empty<ETG.TourItinerary>())
            .Where(itinerary =>
                itinerary != null &&
                itinerary.SelectedFreedomOfChoiceOptions != null &&
                itinerary.SelectedFreedomOfChoiceOptions.Any() &&
                !string.IsNullOrWhiteSpace(itinerary.TourItineraryDayCaption))
            .Select(itinerary => itinerary.TourItineraryDayCaption.Trim())
            .ToList()
            ?? new List<string>();
    }

    private async Task<List<TourUpgradeFolderViewModel>> GetTourUpgradesAsync(Tour tour)
    {
        var folders = tour?.SelectedTourUpgradesFolders?
            .Where(folder => folder != null)
            .ToList() ?? [];

        if (folders.Count == 0)
        {
            return [];
        }

        // 1. Collect GUIDs of the selected upgraded tour pages
        var tourGuids = folders
            .SelectMany(folder => folder.SelectedTourUpdateLinkedDocuments ?? Enumerable.Empty<ETG.TourUpdateLinkedDocument>())
            .Where(doc => doc != null)
            .SelectMany(doc => doc.TourUpgradesTourCode ?? Enumerable.Empty<WebPageRelatedItem>())
            .Where(item => item != null && item.WebPageGuid != Guid.Empty)
            .Select(item => item.WebPageGuid)
            .Distinct()
            .ToArray();

        if (tourGuids.Length == 0)
        {
            return [];
        }

        // 2. Load the tour pages by GUID
        var upgradedTours = (await contentRetriever.RetrievePagesByGuids<Tour>(tourGuids))
            .Where(t => t?.SystemFields != null && !string.IsNullOrWhiteSpace(t.TourCode))
            .GroupBy(t => t.SystemFields.WebPageItemGUID)
            .ToDictionary(g => g.Key, g => g.First());

        if (upgradedTours.Count == 0)
        {
            return [];
        }

        // 3. Prices (bulk, keyed by TourCode) and page URLs, in parallel
        var pricesTask = pricingService.GetPricesBulkAsync(upgradedTours.Values.ToList());

        var urlTasks = upgradedTours.Values
            .Select(async t =>
            {
                var url = await urlRetriever.Retrieve(t);
                return new KeyValuePair<Guid, string>(
                    t.SystemFields.WebPageItemGUID,
                    url?.RelativePath ?? string.Empty);
            })
            .ToArray();

        await Task.WhenAll(pricesTask, Task.WhenAll(urlTasks));

        var prices = await pricesTask;
        var urls = urlTasks.Select(x => x.Result).ToDictionary(x => x.Key, x => x.Value);

        // 4. Map folder -> linked document -> upgraded tour
        var result = new List<TourUpgradeFolderViewModel>();

        foreach (var folder in folders)
        {
            var folderModel = new TourUpgradeFolderViewModel
            {
                Name = folder.DocumentName ?? string.Empty
            };

            foreach (var doc in folder.SelectedTourUpdateLinkedDocuments ?? Enumerable.Empty<ETG.TourUpdateLinkedDocument>())
            {
                if (doc == null) continue;

                foreach (var related in doc.TourUpgradesTourCode ?? Enumerable.Empty<WebPageRelatedItem>())
                {
                    if (related == null ||
                        !upgradedTours.TryGetValue(related.WebPageGuid, out var upgradedTour))
                    {
                        continue;
                    }

                    prices.TryGetValue(upgradedTour.TourCode, out var priceInfo);
                    urls.TryGetValue(related.WebPageGuid, out var pageUrl);

                    folderModel.Items.Add(new TourUpgradeItemViewModel
                    {
                        Label = doc.TourUpgradesLabel ?? string.Empty,
                        IconClass = doc.TourUpgradesIconClass ?? string.Empty,
                        TourName = upgradedTour.TourName ?? string.Empty,
                        TourCode = upgradedTour.TourCode,
                        PageUrl = pageUrl ?? string.Empty,
                        PriceTypeLabel = upgradedTour.TourPriceTypeLabel ?? string.Empty,
                        PriceInfo = priceInfo
                    });
                }
            }

            if (folderModel.Items.Count > 0)
            {
                result.Add(folderModel);
            }
        }

        return result;
    }

    public sealed class TourPageTemplateViewModel
    {
        public Tour Tour { get; set; } = default!;
        public TourPricingResult PriceInfo { get; set; } = new(0, 0);
        public ContactUsViewModel CTA { get; set; } = new();
        public ShareLinksViewModel ShareLinks { get; set; } = new();
        public IEnumerable<Folder> PartnershipFolders { get; set; } = Enumerable.Empty<Folder>();
        public TourListingViewModel FeatureTours { get; set; } = new();
        public IEnumerable<ETG.Core.Content.Experience> Experiences { get; set; }
            = Enumerable.Empty<ETG.Core.Content.Experience>();
        public string CurrentUrl { get; set; } = string.Empty;
        public IEnumerable<string> TourTypeNames { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<Destination> Destinations { get; set; } = Enumerable.Empty<Destination>();
        public PromotionInfo? Promotion { get; set; }
        public ETG.DiscountAndOffer? Discount { get; set; }
        public ReviewListingResponse? Reviews { get; set; }
        public IEnumerable<FAQ> FAQs { get; set; } = Enumerable.Empty<FAQ>();
        public IEnumerable<string> FreedomOfChoiceDays { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<TourUpgradeFolderViewModel> TourUpgrades { get; set; } = Enumerable.Empty<TourUpgradeFolderViewModel>();
    }
    public sealed class TourOfferData
    {
        public PromotionInfo? Promotion { get; set; }
        public ETG.DiscountAndOffer? Discount { get; set; }
    }

    public sealed class TourUpgradeFolderViewModel
    {
        public string Name { get; set; } = string.Empty;   // TourUpgradesFolder.DocumentName
        public List<TourUpgradeItemViewModel> Items { get; set; } = new();
    }

    public sealed class TourUpgradeItemViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string TourName { get; set; } = string.Empty;
        public string TourCode { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public string PriceTypeLabel { get; set; } = string.Empty;
        public TourPricingResult? PriceInfo { get; set; }
    }
}


