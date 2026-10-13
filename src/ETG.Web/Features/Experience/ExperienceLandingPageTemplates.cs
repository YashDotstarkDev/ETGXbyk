using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Features;
using ETG.Web.Features.Experience.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Container = ETG.Core.Content.Container;
using MediaFile = ETG.Core.Content.MediaFile;

[assembly: RegisterWebPageRoute(
    contentTypeName: Container.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.Experience.ExperienceLandingPageTemplateController),
    Path = "/experiences",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Experience;

public class ExperienceLandingPageTemplateProperties
    : IPageTemplateProperties
{
}

public class ExperienceLandingPageTemplateController(
    IMediator mediator,
    IWebPageUrlRetriever urlRetriever,
    IWebsiteChannelContext channelContext,
    IWebPageDataContextRetriever contextRetriever,
    WebPageMetaService metaService
) : Controller
{
    public async Task<ActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(
            new ExperienceLandingPageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName
            )
        );

        if (result?.Page is null)
        {
            return NotFound();
        }

        // Resolve the selected redirect page to its URL.
        var redirectPage = result.Page.ContainerRedirect?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault();

        if (redirectPage is not null)
        {
            var redirectUrl = await urlRetriever.Retrieve(redirectPage);

            if (!string.IsNullOrWhiteSpace(redirectUrl.RelativePath))
            {
                return Redirect(redirectUrl.RelativePath);
            }
        }

        var heroImage = result.Page.ContainerHeroImage?
            .OfType<MediaFile>()
            .FirstOrDefault();

        var items = await Task.WhenAll(
            result.Items.Select(async experience =>
            {
                var url = await urlRetriever.Retrieve(experience);

                var image = experience.ExperienceHeroImage?
                    .OfType<MediaFile>()
                    .FirstOrDefault();

                return new PrimaryLandingListingItemViewModel
                {
                    Title = experience.ExperienceName ?? "",
                    Summary = experience.ExperienceSummary ?? "",
                    LinkPath = url.RelativePath,
                    Image = image is null
                        ? null
                        : ImageAssetViewModel.Create(image),
                    ImageAlt = experience.ExperienceHeroImageAltTag
                        ?? experience.ExperienceName
                        ?? "",
                    CtaText = $"Explore {experience.ExperienceName ?? ""} Holidays"
                };
            })
        );

        // Resolve the selected contact page to its URL.
        var contactPage = result.Page.ContainerHeroContactUsCtaPath?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault();

        var contactUrl = string.Empty;

        if (contactPage is not null)
        {
            var url = await urlRetriever.Retrieve(contactPage);
            contactUrl = url.RelativePath;
        }

        var viewModel = new ExperienceLandingPageViewModel
        {
            Name = result.Page.ContainerName ?? "",

            Hero = new HeroViewModel
            {
                HeroImageUrl = heroImage?.LegacyMediaFileAsset?.Url ?? "",
                HeroImageAltText = result.Page.ContainerHeroImageAltTag ?? "",
                Heading = result.Page.ContainerHeading ?? "",
                HeadingText = result.Page.ContainerHeadingText,
                HeroAccreditation = result.Page.ContainerHeroAccreditation,
                HeroContactUsPath = contactUrl,
                HeroPhone = result.Page.ContainerHeroPhone
            },

            JsonSchema = result.Page.ContainerJsonSchema ?? "",

            PrimaryLandingListing = new PrimaryLandingListingViewModel
            {
                Items = items
            }
        };

        if (result.Page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/Experience/ExperienceLandingPage_Default.cshtml",
            viewModel
        );
    }
}

public class ExperienceLandingPageViewModel
{
    public string Name { get; init; } = "";

    public string JsonSchema { get; init; } = "";

    public PrimaryLandingListingViewModel PrimaryLandingListing { get; init; }

    public HeroViewModel Hero { get; init; } = new();
}