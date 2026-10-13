using CMS.Websites;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;

using ContainerContent = ETG.Core.Content.Container;

[assembly: RegisterPageTemplate(
    identifier: "ETG.CampaignTemplate",
    name: "Campaign Landing Page",
    propertiesType:
        typeof(ETG.Web.Features.CampaignPage.CampaignPageTemplateProperties),
    customViewName:
        "~/Features/CampaignPage/CampaignPage_Default.cshtml",
    ContentTypeNames = [ContainerContent.CONTENT_TYPE_NAME]
)]

namespace ETG.Web.Features.CampaignPage;

public sealed class CampaignPageTemplateProperties
    : IPageTemplateProperties
{
}

public static class CampaignPageTemplate
{
    public static async Task<CampaignPageViewModel?> LoadViewModelAsync(
        IMediator mediator,
        IWebPageDataContextRetriever contextRetriever,
        IWebPageUrlRetriever urlRetriever,
        WebPageMetaService metaService,
        CancellationToken cancellationToken = default)
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return null;
        }

        var page = await mediator.Send(
            new CampaignPageQuery(data.WebPage),
            cancellationToken);

        if (page is null)
        {
            return null;
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
        return await CampaignPageViewModel.CreateAsync(
            page,
            urlRetriever);
    }
}