using CMS.Websites;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "ETG.GenericContentTemplate",
    name: "Generic Content",
    propertiesType:
        typeof(ETG.Web.Features.GenericPage.GenericPageTemplateProperties),
    customViewName:
        "~/Features/GenericPage/GenericPage_Default.cshtml",
    ContentTypeNames = [Container.CONTENT_TYPE_NAME]
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Container.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.GenericPage.GenericPageTemplateController)
)]

namespace ETG.Web.Features.GenericPage;

public sealed class GenericPageTemplateProperties
    : IPageTemplateProperties
{
}

public sealed class GenericPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    IWebPageUrlRetriever urlRetriever,
    WebPageMetaService metaService
) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var page = await mediator.Send(
            new GenericPageQuery(data.WebPage));

        if (page is null)
        {
            return NotFound();
        }

        if (page.ContainerExpiry > DateTime.MinValue
            && DateTime.Now > page.ContainerExpiry)
        {
            var redirectPage = page.ContainerRedirect?
                .OfType<IWebPageFieldsSource>()
                .FirstOrDefault();

            if (redirectPage is not null)
            {
                var redirectUrl = await urlRetriever.Retrieve(
                    redirectPage);

                if (!string.IsNullOrWhiteSpace(redirectUrl.RelativePath))
                {
                    return Redirect(redirectUrl.RelativePath);
                }
            }
        }

        // Retrieve once for both metadata and the hero partial.
        var heroImage = page.ContainerHeroImage?
            .OfType<ETG.Core.Content.MediaFile>()
            .FirstOrDefault();

        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        var heroModel = new HeroViewModel
        {
            HeroImageUrl =
                heroImage?.LegacyMediaFileAsset?.Url ?? string.Empty
        };

        return View(
            "~/Features/GenericPage/GenericPage_Default.cshtml",
            heroModel);
    }
}