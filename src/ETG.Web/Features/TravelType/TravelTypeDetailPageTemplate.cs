using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Features.TravelType.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterPageTemplate(
	identifier: "ETG.TravelTypeDetailTemplate",
	name: "Travel Type Detail - Default",
	propertiesType: typeof(
		ETG.Web.Features.TravelType.TravelTypeDetailPageTemplateProperties),
	customViewName:
		"~/Features/TravelType/TravelTypeDetailPage_Default.cshtml",
	ContentTypeNames = [TravelTypeDetail.CONTENT_TYPE_NAME]
)]

[assembly: RegisterWebPageRoute(
	contentTypeName: TravelTypeDetail.CONTENT_TYPE_NAME,
	controllerType: typeof(
		ETG.Web.Features.TravelType.TravelTypeDetailPageTemplateController),
	WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.TravelType;

public class TravelTypeDetailPageTemplateProperties : IPageTemplateProperties
{
}

public class TravelTypeDetailPageTemplateController(
	IMediator mediator,
	IWebsiteChannelContext channelContext,
	IWebPageDataContextRetriever contextRetriever,
    WebPageMetaService metaService
) : Controller
{
	public async Task<ActionResult> Index()
	{
		if (!contextRetriever.TryRetrieve(out var data))
			return NotFound();

		var result = await mediator.Send(
			new TravelTypeDetailPageQuery(
				data.WebPage,
				channelContext.WebsiteChannelName
			)
		);

		if (result?.Page is null)
			return NotFound();

		var page = result.Page;

		var heroImage = page.TravelTypeDetailHeroImage?
			.OfType<MediaFile>()
			.FirstOrDefault();

		var heading = page.TravelTypeDetailHeading
			?? page.TravelTypeDetailName
			?? "";

		var viewModel = new TravelTypeDetailPageViewModel
		{
			Name = page.TravelTypeDetailName ?? "",
			Heading = heading,
			Summary = page.TravelTypeDetailSummary ?? "",
			Detail = page.TravelTypeDetail1 ?? "",

			Hero = new HeroViewModel
			{
				HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
				HeroImageAltText = page.TravelTypeDetailHeroImageAltTag ?? "",
				Heading = heading,
				HeroIconSvg = page.TravelTypeDetailHeroSVGIcon ?? "",
				DisableOverlay = true
			},

			ThemedPackagesHeading = result.ThemedPackagesHeading,
			ThemedPackagesUrl = result.ThemedPackagesUrl,
			HideGoogleReviews = page.HideGoogleReviews
		};

        if (result.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return new TemplateResult(viewModel);
	}
}

public class TravelTypeDetailPageViewModel
{
	public string Name { get; init; } = "";
	public string Heading { get; init; } = "";
	public string Summary { get; init; } = "";
	public string Detail { get; init; } = "";

	public HeroViewModel Hero { get; init; } = new();

	public string ThemedPackagesHeading { get; init; } = "";
	public string ThemedPackagesUrl { get; init; } = "";
	public bool HideGoogleReviews { get; init; }
	public IEnumerable<ArticleViewModel> RelatedArticles { get; init; } = [];
}