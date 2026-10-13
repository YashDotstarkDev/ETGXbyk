using CMS.Websites;
using ETG.Core.Content;
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

        if (page is IMetaDataSchema metaFields)
        {
            var heroImage = page.ContainerHeroImage?
                .OfType<ETG.Core.Content.MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View("~/Features/GenericPage/GenericPage_Default.cshtml");
    }
}