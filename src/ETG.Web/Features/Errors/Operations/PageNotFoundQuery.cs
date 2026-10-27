using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Models.Tour;
using ETG.Web.Features;
using ETG.Web.Services.Tour;
using Kentico.Content.Web.Mvc;
using MediatR;
using Microsoft.Extensions.Configuration;
using ArticleContent = ETG.Core.Content.Article;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Web.Features.Errors;

public record PageNotFoundQuery(int TourCount = 8, int ArticleCount = 3) : IRequest<PageNotFoundViewModel>;

public class PageNotFoundViewModel
{
    public int StatusCode { get; set; } = 404;
    public TourListingViewModel Tours { get; set; } = new();
    public IEnumerable<ArticleViewModel> Articles { get; set; } = [];
    public string SearchAppId { get; set; } = string.Empty;
    public string SearchApiKey { get; set; } = string.Empty;
    public string SearchIndexName { get; set; } = string.Empty;
}

public class PageNotFoundQueryHandler(
    IContentQueryExecutor executor,
    IWebsiteChannelContext channelContext,
    IWebPageUrlRetriever urlRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    ITourSummaryService tourSummaryService,
    ITourPricingService tourPricingService,
    IConfiguration configuration)
    : IRequestHandler<PageNotFoundQuery, PageNotFoundViewModel>
{
    public async Task<PageNotFoundViewModel> Handle(
        PageNotFoundQuery request,
        CancellationToken cancellationToken = default)
    {
        var channelName = channelContext.WebsiteChannelName;

        var toursTask = GetToursAsync(channelName, request.TourCount, cancellationToken);
        var articlesTask = GetArticlesAsync(channelName, request.ArticleCount, cancellationToken);

        await Task.WhenAll(toursTask, articlesTask);

        var algoliaSection = configuration.GetSection("CMSAlgoliaSearch");
        var searchAppId = algoliaSection["ApplicationId"] ?? string.Empty;
        var searchApiKey = algoliaSection["SearchKey"] ?? string.Empty;
        var searchIndexName = algoliaSection["SearchIndexTour"] ?? string.Empty;

        return new PageNotFoundViewModel
        {
            StatusCode = 404,
            Tours = await toursTask,
            Articles = await articlesTask,
            SearchAppId = searchAppId,
            SearchApiKey = searchApiKey,
            SearchIndexName = searchIndexName
        };
    }

    private async Task<TourListingViewModel> GetToursAsync(string channelName, int count, CancellationToken cancellationToken)
    {
        try
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    TourContent.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelName)
                        .WithLinkedItems(4)
                        .Where(where => where
                            .WhereNull("TourIsUpgrade")
                            .Or()
                            .WhereNotEquals("TourIsUpgrade", 1))
                );

            var allTours = await executor.GetMappedWebPageResult<TourContent>(query, cancellationToken: cancellationToken);

            // Fetch the 8 most recently published tours (same logic as articles / publish date)
            var recentTours = allTours
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.TourCode))
                .OrderByDescending(t => t.SystemFields.ContentItemCommonDataLastPublishedWhen ?? DateTime.MinValue)
                .ThenByDescending(t => t.SystemFields.ContentItemID)
                .Take(count)
                .ToList();

            // Bulk prices evaluated via ITourPricingService
            // Automatically converts AUD to active currency (AUD, NZD, USD, GBP, EUR) with appropriate symbol and code
            var bulkPrices = await tourPricingService.GetPricesBulkAsync(recentTours);

            var tourViewModels = new List<(TourSummaryViewModel ViewModel, int Price)>();

            foreach (var tour in recentTours)
            {
                var url = await urlRetriever.Retrieve(tour);
                var priceInfo = bulkPrices.TryGetValue(tour.TourCode, out var p)
                    ? p
                    : await tourPricingService.GetPriceAsync(tour);

                var vm = await tourSummaryService.MapToViewModelAsync(tour, url.RelativePath, priceInfo);

                tourViewModels.Add((vm, priceInfo.CurrentPrice));
            }

            // Show them according to price ascending order
            var sortedTours = tourViewModels
                .OrderBy(t => t.Price)
                .Select(t => t.ViewModel)
                .ToList();

            return new TourListingViewModel
            {
                Heading = "Packages",
                SectionId = "not-found-packages",
                Tours = sortedTours,
                HideViewAllButton = false,
                ViewAllUrl = "/search?from=404page",
                ViewAllButtonLabel = "View all"
            };
        }
        catch
        {
            return new TourListingViewModel();
        }
    }

    private async Task<IEnumerable<ArticleViewModel>> GetArticlesAsync(string channelName, int count, CancellationToken cancellationToken)
    {
        try
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    ArticleContent.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelName)
                        .WithLinkedItems(1)
                        .OrderBy(new OrderByColumn(nameof(ArticleContent.ArticleDate), OrderDirection.Descending))
                        .TopN(count)
                );

            var articles = await executor.GetMappedWebPageResult<ArticleContent>(query, cancellationToken: cancellationToken);

            var articleList = new List<ArticleViewModel>();
            foreach (var article in articles)
            {
                var vm = await ArticleViewModel.CreateAsync(article, urlRetriever, taxonomyRetriever);
                articleList.Add(vm);
            }

            return articleList;
        }
        catch
        {
            return [];
        }
    }
}
