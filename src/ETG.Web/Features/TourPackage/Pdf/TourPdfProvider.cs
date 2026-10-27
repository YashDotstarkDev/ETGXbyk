using System.Text.RegularExpressions;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using ETG.Core.Models;
using ETG.Core.Modules;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models.Tour;
using ETG.Web.Services.Tour;
using ETG.Web.Services.Storage;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ETGBooking;
using static ETG.Web.Features.TourPackage.TourPageTemplateController;

namespace ETG.Web.Features.TourPackage.Pdf;

public sealed class TourPdfProvider(
    IContentQueryExecutor executor,
    IWebsiteChannelContext channelContext,
    IWebPageUrlRetriever urlRetriever,
    IPDFGenerator pdfGenerator,
    IBlobStorageService blobStorageService,
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    IRazorViewEngine razorViewEngine,
    ITempDataProvider tempDataProvider,
    IServiceProvider serviceProvider,
    ILogger<TourPdfProvider> logger) : ITourPdfProvider
{
    private const string PdfDirectoryName = "etg/pdf";
    private const string PrintViewPath = "~/Features/TourPackage/Print.cshtml";

    private readonly IContentQueryExecutor executor = executor;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;
    private readonly IPDFGenerator pdfGenerator = pdfGenerator;
    private readonly IBlobStorageService blobStorageService = blobStorageService;
    private readonly IWebHostEnvironment environment = environment;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly IRazorViewEngine razorViewEngine = razorViewEngine;
    private readonly ITempDataProvider tempDataProvider = tempDataProvider;
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<TourPdfProvider> logger = logger;

    public async Task<TourPdf?> GetTourPdfAsync(
        string tourCode,
        bool forceRegenerate = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tourCode))
        {
            logger.LogWarning("TourCode was not supplied for PDF generation.");
            return null;
        }

        tourCode = tourCode.Trim();

        if (!IsValidTourCode(tourCode))
        {
            logger.LogWarning("Invalid TourCode supplied: {TourCode}", tourCode);
            return null;
        }

        try
        {
            var pdfRelativePath = $"pdf/{tourCode}.pdf";

            // Load the Tour content item first (needed for staleness check)
            var query = new ContentItemQueryBuilder()
                .ForContentType(Tour.CONTENT_TYPE_NAME, config =>
                {
                    config.ForWebsite(channelContext.WebsiteChannelName).WithLinkedItems(4);
                    config.Where(w => w.WhereEquals(nameof(Tour.TourCode), tourCode));
                });

            var tours = await executor.GetMappedWebPageResult<Tour>(
                query, cancellationToken: cancellationToken);
            var tour = tours.FirstOrDefault();

            if (tour == null)
            {
                logger.LogWarning("Tour not found. TourCode: {TourCode}", tourCode);
                return null;
            }

            // Serve cached PDF from blob if it exists and is up-to-date
            if (!forceRegenerate)
            {
                var blobLastModified = await blobStorageService.GetLastModifiedAsync(pdfRelativePath);
                if (blobLastModified.HasValue)
                {
                    var tourLastPublished = tour.SystemFields.ContentItemCommonDataLastPublishedWhen;
                    bool isStale = tourLastPublished.HasValue &&
                                   tourLastPublished.Value > blobLastModified.Value.UtcDateTime;

                    if (!isStale)
                    {
                        var cachedStream = await blobStorageService.OpenReadAsync(pdfRelativePath);
                        if (cachedStream != null)
                        {
                            logger.LogInformation(
                                "Returning cached Tour PDF. TourCode: {TourCode}", tourCode);
                            return new TourPdf(cachedStream, tourCode);
                        }
                    }
                    else
                    {
                        logger.LogInformation(
                            "Cached PDF is stale (tour updated {TourLastPublished}, blob {BlobLastModified}). Regenerating. TourCode: {TourCode}",
                            tourLastPublished, blobLastModified, tourCode);
                    }
                }
            }

            // Build the full ViewModel (same data as the live page)
            var viewModel = await BuildViewModelAsync(tour, cancellationToken);

            // Render Print.cshtml to HTML string in-process — no HTTP round-trip
            var baseUrl = GetBaseUrl();
            var html = await RenderViewToStringAsync(viewModel, cancellationToken);

            if (string.IsNullOrWhiteSpace(html))
            {
                logger.LogError("Rendered HTML is empty. TourCode: {TourCode}", tourCode);
                return null;
            }

            logger.LogInformation(
                "HTML rendered. TourCode: {TourCode}, Length: {Length}", tourCode, html.Length);

            // Replace tilde and root-relative paths with absolute URLs
            // so ABCpdf Chrome86 can fetch all local assets (CSS, images, fonts).
            // Uses regex to catch every src="/..." and href="/..." in one pass.
            html = RewriteRelativeUrls(html, baseUrl);

            // Generate PDF from HTML string
            var stream = await pdfGenerator.GeneratePdfFromHtmlAsync(
                html, baseUrl, cancellationToken);

            if (stream == null || stream.Length == 0)
            {
                logger.LogError("Generated PDF stream is empty. TourCode: {TourCode}", tourCode);
                return null;
            }

            try
            {
                // Save to Azure Blob Storage (or local disk fallback)
                stream.Position = 0;
                await blobStorageService.SaveFileAsync(stream, pdfRelativePath, "application/pdf");

                logger.LogInformation("Tour PDF saved to storage. TourCode: {TourCode}", tourCode);

                // Fetch stream from storage to serve to user
                var savedStream = await blobStorageService.OpenReadAsync(pdfRelativePath);
                if (savedStream != null)
                {
                    await stream.DisposeAsync();
                    return new TourPdf(savedStream, tourCode);
                }

                stream.Position = 0;
                return new TourPdf(stream, tourCode);
            }
            catch
            {
                await stream.DisposeAsync();
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Tour PDF generation cancelled. TourCode: {TourCode}", tourCode);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate Tour PDF. TourCode: {TourCode}", tourCode);
            throw; // propagate so the controller can surface detail in Development
        }
    }

    private async Task<string> RenderViewToStringAsync(
        object model,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext unavailable.");

        var actionContext = new ActionContext(
            httpContext,
            httpContext.GetRouteData() ?? new RouteData(),
            new ActionDescriptor());

        await using var sw = new StringWriter();

        var viewResult = razorViewEngine.GetView(null, PrintViewPath, false);
        if (!viewResult.Success)
            viewResult = razorViewEngine.FindView(actionContext, PrintViewPath, false);

        if (!viewResult.Success)
            throw new InvalidOperationException(
                $"Could not find Razor view '{PrintViewPath}'. " +
                $"Searched: {string.Join(", ", viewResult.SearchedLocations ?? [])}");

        var viewDictionary = new ViewDataDictionary(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary()) { Model = model };

        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        var viewContext = new ViewContext(
            actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

    private async Task<TourPageTemplateViewModel> BuildViewModelAsync(
        Tour tour,
        CancellationToken cancellationToken)
    {
        var pricingService    = serviceProvider.GetRequiredService<ITourPricingService>();
        var tourSummaryService = serviceProvider.GetRequiredService<ITourSummaryService>();
        var shareLinksService  = serviceProvider.GetRequiredService<IShareLinksService>();
        var tourTypeProvider   = serviceProvider.GetRequiredService<IInfoProvider<TourTypeInfo>>();
        var contentRetriever   = serviceProvider.GetRequiredService<IContentRetriever>();
        var promotionProvider  = serviceProvider.GetRequiredService<IInfoProvider<PromotionInfo>>();
        var mediator           = serviceProvider.GetRequiredService<IMediator>();
        var urlRetrieverLocal  = serviceProvider.GetRequiredService<IWebPageUrlRetriever>();

        var priceInfo = await pricingService.GetPriceAsync(tour);

        var tourTypeCodeNames = tour.TourTypes?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        var tourTypeNames = new List<string>();
        if (tourTypeCodeNames.Length > 0)
        {
            tourTypeNames = (await tourTypeProvider
                .Get()
                .WhereIn(nameof(TourTypeInfo.TourTypeCodeName), tourTypeCodeNames)
                .GetEnumerableTypedResultAsync())
                .Select(x => x.TourTypeName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        var experiences  = await GetExperiencesAsync(tour.TourExperiences, cancellationToken);
        var destinations = await GetDestinationsAsync(tour.TourPrimaryCountry, contentRetriever);

        var partnershipFolders = new List<Folder>();
        if (tour.TourInPartnershipPath?.Any() == true)
        {
            var guids = tour.TourInPartnershipPath
                .Where(x => x != null && x.WebPageGuid != Guid.Empty)
                .Select(x => x.WebPageGuid).ToList();

            if (guids.Count > 0)
            {
                var pq = new ContentItemQueryBuilder()
                    .ForContentType(Folder.CONTENT_TYPE_NAME, cfg =>
                    {
                        cfg.WithLinkedItems(4);
                        cfg.Where(w => w.WhereIn("ContentItemGUID", guids));
                    });
                partnershipFolders = (await executor.GetMappedWebPageResult<Folder>(
                    pq, cancellationToken: cancellationToken)).ToList();
            }
        }

        var relatedTours = await mediator.Send(
            new Operations.GetRelatedToursQuery(tour), cancellationToken);

        var mappedTours = new List<TourSummaryViewModel>();
        if (relatedTours?.Count > 0)
        {
            foreach (var rt in relatedTours)
            {
                if (rt == null) continue;
                var u = await urlRetrieverLocal.Retrieve(rt);
                mappedTours.Add(await tourSummaryService.MapToViewModelAsync(
                    rt, u?.RelativePath ?? string.Empty, null));
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

        var promotion   = await GetTourPromotionAsync(tour.TourCode, promotionProvider);
        var shareLinks  = shareLinksService.GetShareLinks();
        var request     = httpContextAccessor.HttpContext?.Request;
        var currentUrl  = request != null
            ? Uri.EscapeDataString(
                $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}")
            : string.Empty;

        return new TourPageTemplateViewModel
        {
            Tour             = tour,
            CTA              = new ContactUsViewModel { CTAUrl = $"/enquire/{tour.TourCode}" },
            PriceInfo        = priceInfo,
            FeatureTours     = featureTours,
            Experiences      = experiences,
            Destinations     = destinations,
            PartnershipFolders = partnershipFolders,
            ShareLinks       = shareLinks,
            CurrentUrl       = currentUrl,
            TourTypeNames    = tourTypeNames,
            Promotion        = promotion
        };
    }

    private async Task<IEnumerable<ETG.Core.Content.Experience>> GetExperiencesAsync(
        IEnumerable<WebPageRelatedItem> selectedItems,
        CancellationToken cancellationToken)
    {
        var guids = selectedItems?
            .Where(x => x != null && x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid).Distinct().ToArray() ?? [];

        if (guids.Length == 0) return [];

        var query = new ContentItemQueryBuilder()
            .ForContentType(ETG.Core.Content.Experience.CONTENT_TYPE_NAME,
                config => config.ForWebsite("ETG"));

        var pages = await executor.GetMappedWebPageResult<ETG.Core.Content.Experience>(
            query, cancellationToken: cancellationToken);

        var byGuid = pages.ToDictionary(x => x.SystemFields.WebPageItemGUID);
        return guids.Where(g => byGuid.ContainsKey(g)).Select(g => byGuid[g]).ToList();
    }

    private static async Task<IEnumerable<Destination>> GetDestinationsAsync(
        IEnumerable<WebPageRelatedItem> selectedItems,
        IContentRetriever contentRetriever)
    {
        var guids = selectedItems?
            .Where(x => x != null && x.WebPageGuid != Guid.Empty)
            .Select(x => x.WebPageGuid).Distinct().ToArray() ?? [];

        if (guids.Length == 0) return [];
        return (await contentRetriever.RetrievePagesByGuids<Destination>(guids)).ToList();
    }

    private static async Task<PromotionInfo?> GetTourPromotionAsync(
        string tourCode,
        IInfoProvider<PromotionInfo> promotionProvider)
    {
        if (string.IsNullOrWhiteSpace(tourCode)) return null;

        foreach (var p in await promotionProvider.Get().GetEnumerableTypedResultAsync())
        {
            if (string.IsNullOrWhiteSpace(p.PromotionPackageCodes)) continue;
            var codes = p.PromotionPackageCodes
                .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());
            if (codes.Any(x => string.Equals(x, tourCode, StringComparison.OrdinalIgnoreCase)))
                return p;
        }
        return null;
    }

    private string GetBaseUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("HTTP request is unavailable.");
        return $"{request.Scheme}://{request.Host}";
    }

    private static bool IsValidTourCode(string tourCode) =>
        tourCode.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

    /// <summary>
    /// Rewrites all root-relative src/href/url() paths to absolute URLs
    /// so the headless Chrome renderer inside ABCpdf can fetch every asset.
    /// Already-absolute URLs (http:// / https://) are left untouched.
    /// </summary>
    private static string RewriteRelativeUrls(string html, string baseUrl)
    {
        // Tilde paths from Razor (~/images/, ~/css/, etc.)
        html = html.Replace("~/", $"{baseUrl}/");

        // src="/..." and href="/..." attributes
        html = Regex.Replace(
            html,
            @"(src|href)=""(/[^""]*?)""",
            m => $"{m.Groups[1].Value}=\"{baseUrl}{m.Groups[2].Value}\"");

        // CSS url('/...') and url("/...") — covers background images in inline styles
        html = Regex.Replace(
            html,
            @"url\(['""]?(/[^'"")\s]+)['""]?\)",
            m => $"url('{baseUrl}{m.Groups[1].Value}')");

        return html;
    }
}
