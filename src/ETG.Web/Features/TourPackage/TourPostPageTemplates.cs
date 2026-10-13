using AngleSharp.Dom;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Core.Models.Tour;
using ETG.Core.Modules;
using ETG.Web.Features.TourPackage;
using ETG.Web.Infrastructure;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using ETGBooking;
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
    private readonly IInfoProvider<PromotionInfo> promotionProvider = promotionProvider;

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

        //if (tour is IWebPageMetaFields metaFields)
        //{
        //    metaService.SetMeta(metaFields);
        //}

        var destinations = await GetDestinations(tour.TourPrimaryCountry);
        var promotion = await GetTourPromotions(tour.TourCode);

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

        var experiences = await GetExperiences(tour.TourExperiences);

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


        // Related Tour Packages for ETG.Tour Tour Code
        var relatedTours = await mediator.Send(
            new Operations.GetRelatedToursQuery(tour),
            HttpContext.RequestAborted);

        var mappedTours = new List<TourSummaryViewModel>();

        if (relatedTours != null && relatedTours.Count > 0)
        {
            foreach (var relatedTour in relatedTours)
            {
                if (relatedTour == null)
                {
                    continue;
                }

                var url = await urlRetriever.Retrieve(relatedTour);
                var tourViewModel = await tourSummaryService.MapToViewModelAsync(relatedTour, url?.RelativePath ?? string.Empty, null);
                mappedTours.Add(tourViewModel);
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

        var request = httpContextAccessor.HttpContext?.Request;

        var currentUrl =
            $"{request.Scheme}://{request.Host}" +
            $"{request.PathBase}" +
            $"{request.Path}" +
            $"{request.QueryString}";
        var encodedUrl =
           Uri.EscapeDataString(currentUrl);

        var ctaurl = new ContactUsViewModel
        {
            CTAUrl = $"/enquire/{tour.TourCode}"
        };

        //var tourViewModel = await tourSummaryService.MapToViewModelAsync(relatedTour, url?.RelativePath ?? string.Empty, null);

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
            Promotion = promotion
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
                if (packageCodes.Any(x =>  string.Equals(x, tourCode, StringComparison.OrdinalIgnoreCase)))
                {
                    return promotion;
                }
            }
        }

            return null;
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
    }
}


