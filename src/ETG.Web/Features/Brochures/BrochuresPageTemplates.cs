using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Brochures.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using MediaFile = ETG.Core.Content.MediaFile;
using PageContent = ETG.Core.Content.Page;

[assembly: RegisterPageTemplate(
    identifier: "Website.BrochuresPage_Default",
    name: "Brochures",
    propertiesType:
        typeof(ETG.Web.Features.Brochures.BrochuresPageTemplateProperties),
    customViewName:
        "~/Features/Brochures/BrochuresPage_Default.cshtml",
    ContentTypeNames = [PageContent.CONTENT_TYPE_NAME]
)]
[assembly: RegisterWebPageRoute(
    contentTypeName: PageContent.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.Brochures.BrochuresPageTemplateController),
    Path = "/Brochures"
)]
namespace ETG.Web.Features.Brochures;
public class BrochuresPageTemplateProperties : IPageTemplateProperties
{
}
public class BrochuresPageTemplateController(
    IMediator mediator,
    IWebPageDataContextRetriever contextRetriever,
    WebPageMetaService metaService
    ) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var data))
        {
            return NotFound();
        }

        var result = await mediator.Send(
            new BrochuresPageQuery(data.WebPage));

        var cards = result.Folders
            .SelectMany(
                folder => folder.SelectedBrochures ?? [],
                (folder, brochure) => new
                {
                    Folder = folder,
                    Brochure = brochure
                })
            .Select(item =>
            {
                var coverMediaFile = item.Brochure.BrochureCoverImage?
                    .OfType<MediaFile>()
                    .FirstOrDefault();

                var documentMediaFile = item.Brochure.BrochureFile?
                    .OfType<MediaFile>()
                    .FirstOrDefault();

                var cover = coverMediaFile is null
                    ? null
                    : ImageAssetViewModel.Create(coverMediaFile);

                var documentUrl =
                    documentMediaFile?.LegacyMediaFileAsset?.Url;

                var name = !string.IsNullOrWhiteSpace(
                    item.Brochure.BrochureName)
                        ? item.Brochure.BrochureName
                        : item.Brochure.DocumentName;

                return new BrochureCardViewModel
                {
                    Name = name ?? string.Empty,

                    CoverImage = cover,

                    CoverAlt = !string.IsNullOrWhiteSpace(
                        coverMediaFile?.LegacyMediaFileTitle)
                            ? coverMediaFile.LegacyMediaFileTitle
                            : name ?? string.Empty,


                    ViewUrl = !string.IsNullOrWhiteSpace(documentUrl)
                        ? documentUrl
                        : item.Brochure.BrochureIssuuLink,

                    IsComingSoon = item.Brochure.IsComingSoon
                };
            
            })
            .ToList();

        var viewModel = new BrochuresPageViewModel
        {
            Heading = !string.IsNullOrWhiteSpace(
                result.Page.PageHeading)
                    ? result.Page.PageHeading
                    : result.Page.DocumentName ?? string.Empty,

            IntroHtml = result.Page.PageContent ?? string.Empty,

            Brochures = cards
        };

        if (result.Page is IMetaDataSchema metaFields)
        {
            var heroImage = result.Page.PageHeroImage?
                .OfType<MediaFile>()
                .FirstOrDefault();

            metaService.SetMeta(
                metaFields,
                heroImage?.LegacyMediaFileAsset?.Url);
        }

        return View(
            "~/Features/Brochures/BrochuresPage_Default.cshtml",
            viewModel);
    }
}
public class BrochuresPageViewModel
{
    public string Heading { get; init; } = string.Empty;
    public string IntroHtml { get; init; } = string.Empty;
    public IReadOnlyList<BrochureCardViewModel> Brochures { get; init; } = [];
}
public class BrochureCardViewModel
{
    public string Name { get; init; } = string.Empty;
    public ImageAssetViewModel? CoverImage { get; init; }
    public string CoverAlt { get; init; } = string.Empty;
    public string? ViewUrl { get; init; }
    public bool IsComingSoon { get; init; }
    public bool CanView =>
        !IsComingSoon &&
        !string.IsNullOrWhiteSpace(ViewUrl);
}