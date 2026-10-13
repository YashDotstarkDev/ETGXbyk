using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageTemplate(
    identifier: "ETG.ContactUsFormPageTemplate",
    name: "Contact Us Form Page",
    propertiesType: typeof(
        ETG.Web.Features.ContactUs.ContactUsFormPageTemplateProperties),
    customViewName: "~/Features/ContactUs/ContactUsFormPage.cshtml",
    ContentTypeNames = new[] { Page.CONTENT_TYPE_NAME }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: Page.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.ContactUs.ContactUsFormPageTemplateController),
    Path = "/contact-us",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.ContactUs;

public sealed class ContactUsFormPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class ContactUsFormPageTemplateController(
    IWebPageDataContextRetriever contextRetriever,
    IContentQueryExecutor queryExecutor,
    IWebsiteChannelContext channelContext,
    ILogger<ContactUsFormPageTemplateController> logger,
    WebPageMetaService metaService) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            if (!contextRetriever.TryRetrieve(out var data) || data?.WebPage == null)
            {
                logger.LogWarning(
                    "Unable to retrieve the current Contact Us page context.");

                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName))
            {
                logger.LogWarning(
                    "Website channel name is missing while loading the Contact Us page.");

                return NotFound();
            }

            Page? page = null;

            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    Page.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelContext.WebsiteChannelName)
                        .WithLinkedItems(1)
                        .Where(w =>
                            w.WhereEquals(
                                "WebPageItemID",
                                data.WebPage.WebPageItemID)));

            var result = await queryExecutor.GetMappedWebPageResult<Page>(
                query,
                cancellationToken: HttpContext.RequestAborted);

            page = result?.FirstOrDefault();

            if (page == null)
            {
                logger.LogWarning(
                    "Contact Us page could not be found. WebPageItemID: {WebPageItemID}",
                    data.WebPage.WebPageItemID);

                return NotFound();
            }

            var viewModel = new ContactUsPageViewModel
            {
                Page = page,
                Form = new ContactUsFormViewModel
                {
                    PreferredContactType = "Email"
                }
            };

            if (page is IMetaDataSchema metaFields)
            {
                var heroImage = page.PageHeroImage?
                    .OfType<ETG.Core.Content.MediaFile>()
                    .FirstOrDefault();

                metaService.SetMeta(
                    metaFields,
                    heroImage?.LegacyMediaFileAsset?.Url);
            }

            return View("~/Features/ContactUs/ContactUsFormPage.cshtml", viewModel);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "Contact Us page request was cancelled.");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while loading the Contact Us page.");

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}