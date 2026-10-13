using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content.Pages;
using ETG.Web.Features;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using ArticleContent = ETG.Core.Content.Article;
using ExperiencePage = ETG.Core.Content.Experience;
using MediaFile = ETG.Core.Content.MediaFile;
using Icons = Kentico.Xperience.Admin.Base.Icons;
using ETG.Core.Content;

[assembly: RegisterPageTemplate(
    identifier: "Website.HomePage_Default",
    name: "Home Page - Default",
    propertiesType: typeof(ETG.Web.Features.Home.HomePageTemplateProperties),
    customViewName: "~/Features/Home/HomePage_Default.cshtml",
    ContentTypeNames = [Homepage.CONTENT_TYPE_NAME],
    IconClass = Icons.Home
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Homepage.CONTENT_TYPE_NAME,
    controllerType: typeof(ETG.Web.Features.Home.HomePageTemplateController)
)]

namespace ETG.Web.Features.Home;

public sealed class HomePageTemplateProperties : IPageTemplateProperties
{
}

public sealed class HomePageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IContentQueryExecutor contentQueryExecutor,
    IWebsiteChannelContext websiteChannelContext,
    IWebPageUrlRetriever urlRetriever,
    ITaxonomyRetriever taxonomyRetriever,
    IConfiguration configuration,
    WebPageMetaService metaService
    ) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data)) return NotFound();

        var page = await mediator.Send(new HomePageQuery(data.WebPage));
        if (page is null) return NotFound();

        var selectedExperienceGuids = page.HomepageExperiences?
            .Select(item => item.WebPageGuid)
            .Where(guid => guid != Guid.Empty)
            .Distinct().ToArray() ?? [];

        var experiences = await GetSelectedExperiences(selectedExperienceGuids);

        var relatedArticlesQuery = new ContentItemQueryBuilder();
        relatedArticlesQuery.ForContentType(
            ArticleContent.CONTENT_TYPE_NAME,
            query => query.ForWebsite(websiteChannelContext.WebsiteChannelName).WithLinkedItems(4));

        var relatedArticles = await contentQueryExecutor.GetMappedWebPageResult<ArticleContent>(relatedArticlesQuery);

        var relatedArticleViewModels = await Task.WhenAll(
            relatedArticles
                .OrderByDescending(article => article.ArticleDate)
                .Take(3)
                .Select(article => ArticleViewModel.CreateAsync(article, urlRetriever, taxonomyRetriever)));

        // --- ALGOLIA CONFIG ---
        var algoliaSection = configuration.GetSection("CMSAlgoliaSearch");
        var searchAppId = algoliaSection["ApplicationId"] ?? string.Empty;
        var searchApiKey = algoliaSection["SearchKey"] ?? string.Empty;
        var searchIndexName = algoliaSection["SearchIndexTour"] ?? string.Empty;

        var model = new HomePageViewModel
        {
            HeroCaption = page.HomepageHeroCaption ?? string.Empty,
            HeroSubCaption = page.HomepageHeroSubCaption ?? string.Empty,
            HeroImage = ToImage(page.HomepageHeroImage),
            HeroVideoPoster = ToImage(page.HomepageHeroVideoImage),
            FeaturedTilesTitle = page.HomepageFeaturedTilesTitle ?? string.Empty,
            ProofPointsTitle = page.HomepageProofPointsTitle ?? string.Empty,
            FeatureToursTitle = page.HomepageFeatureToursSectionTitle ?? string.Empty,
            FeatureToursTitle2 = page.HomepageFeatureToursSectionTitle2 ?? string.Empty,
            DestinationsTitle = page.HomepageDestinationsSectionTitle ?? string.Empty,
            FeatureToursViewAllUrl = page.HomepageFeatureToursViewAllUrl ?? string.Empty,
            FeatureToursViewAllUrl2 = page.HomepageFeatureToursViewAllUrl2 ?? string.Empty,
            FeatureToursViewAllLabel2 = page.HomepageFeatureToursViewAllLabel2 ?? string.Empty,
            BlogsTitle = page.HomepageTravelBlogsSectionTitle ?? string.Empty,
            BlogsDescription = page.HomepageTravelBlogsSectionDescription ?? string.Empty,
            BlogsViewAllUrl = page.HomepageTravelBlogsSectionViewAllUrl ?? string.Empty,
            RelatedArticles = relatedArticleViewModels,
            SelectedExperienceGuids = selectedExperienceGuids,
            Experiences = experiences,
            ShowCompetitionPopup = page.HomepageShowCompetitionPopup,
            CompetitionTitle = page.HomepagePopupTitle ?? string.Empty,           
            HomepageJsonSchema =
                page.HomepageJsonSchema ?? string.Empty,
            SearchAppId = searchAppId,
            SearchApiKey = searchApiKey,
            SearchIndexName = searchIndexName
        };

        if (page is IMetaDataSchema metaFields)
        {
            var heroImage = page.HomepageHeroImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View("~/Features/Home/HomePage_Default.cshtml", model);
    }

    private async Task<IReadOnlyList<HomeExperienceViewModel>> GetSelectedExperiences(IReadOnlyList<Guid> selectedGuids)
    {
        if (selectedGuids.Count == 0) return [];

        var query = new ContentItemQueryBuilder();
        query.ForContentType(ExperiencePage.CONTENT_TYPE_NAME,
            parameters => parameters.ForWebsite(websiteChannelContext.WebsiteChannelName));

        var pages = await contentQueryExecutor.GetMappedWebPageResult<ExperiencePage>(query);
        var pagesByGuid = pages.ToDictionary(e => e.SystemFields.WebPageItemGUID);

        var results = new List<HomeExperienceViewModel>();
        foreach (var guid in selectedGuids)
        {
            if (!pagesByGuid.TryGetValue(guid, out var experience)) continue;

            results.Add(new HomeExperienceViewModel
            {
                Id = guid,
                Name = experience.ExperienceName ?? string.Empty,
                Url = experience.GetUrl().RelativePath,
                SvgIcon = experience.ExperienceSVGIcon ?? string.Empty
            });
        }

        if (pages is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields);
        }
        return results;
    }

    private static ImageAssetViewModel? ToImage(IEnumerable<IContentItemFieldsSource>? items)
    {
        var media = items?.OfType<MediaFile>().FirstOrDefault();
        return media is null ? null : ImageAssetViewModel.Create(media);
    }

    private static IReadOnlyList<Guid> ParseGuids(string? value) =>
        (value ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Guid.TryParse(part, out var guid) ? (Guid?)guid : null)
            .Where(guid => guid.HasValue)
            .Select(guid => guid!.Value).ToArray();
}