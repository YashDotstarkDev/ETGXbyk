using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using ContactContent = ETG.Core.Content.Contact;
using PageContent = ETG.Core.Content.Page;

[assembly: RegisterPageTemplate(
    identifier: "ETG.FAQsPage_Default",
    name: "FAQs Page - Default",
    propertiesType:
        typeof(ETG.Web.Features.FAQs.FAQsPageTemplateProperties),
    customViewName: "~/Features/FAQs/FAQsPage_Default.cshtml",
    ContentTypeNames = new[] { PageContent.CONTENT_TYPE_NAME }
)]

[assembly: RegisterWebPageRoute(
    contentTypeName: PageContent.CONTENT_TYPE_NAME,
    controllerType:
        typeof(ETG.Web.Features.FAQs.FAQsPageTemplateController),
    Path = "/faqs",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.FAQs;

public sealed class FAQsPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class FAQsPageTemplateController : Controller
{
    private const string LanguageName = "en-AU";

    private readonly IWebPageDataContextRetriever contextRetriever;
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly WebPageMetaService metaService;

    public FAQsPageTemplateController(
        IWebPageDataContextRetriever contextRetriever,
        IContentQueryExecutor contentQueryExecutor,
        IWebsiteChannelContext websiteChannelContext,
        WebPageMetaService metaService
        )
    {
        this.contextRetriever = contextRetriever;
        this.contentQueryExecutor = contentQueryExecutor;
        this.websiteChannelContext = websiteChannelContext;
        this.metaService = metaService;
    }

    public async Task<IActionResult> Index()
    {
        if (!contextRetriever.TryRetrieve(out var context))
        {
            return NotFound();
        }

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                PageContent.CONTENT_TYPE_NAME,
                parameters => parameters
                    .ForWebsite(websiteChannelContext.WebsiteChannelName)
                    .Where(where => where.WhereEquals(
                        "WebPageItemID",
                        context.WebPage.WebPageItemID)))
            .InLanguage(LanguageName);

        var pages = await contentQueryExecutor
            .GetMappedWebPageResult<PageContent>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = websiteChannelContext.IsPreview
                });

        var page = pages.FirstOrDefault();

        if (page is null)
        {
            return NotFound();
        }

        var contact = await GetGeneralContactAsync();

        var heading = string.IsNullOrWhiteSpace(page.PageHeading)
            ? page.DocumentName
            : page.PageHeading;

        var viewModel = new FAQsPageViewModel
        {
            Heading = string.IsNullOrWhiteSpace(heading)
                ? "FAQs"
                : heading,

            Content = page.PageContent ?? string.Empty,

            JsonSchema = page.PageJsonSchema ?? string.Empty,

            ContactDetails = new GenericSideContactViewModel
            {
                GeneralContact = contact
            }
        };

        ViewBag.HideFaqsButton = true;

        if (page is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields);
        }

        return View("~/Features/FAQs/FAQsPage_Default.cshtml",viewModel);
        //return new TemplateResult(viewModel);
    }

    private async Task<ContactViewModel?> GetGeneralContactAsync()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(ContactContent.CONTENT_TYPE_NAME)
            .InLanguage(LanguageName);

        var contacts = (await contentQueryExecutor
            .GetMappedResult<ContactContent>(
                query,
                new ContentQueryExecutionOptions
                {
                    ForPreview = websiteChannelContext.IsPreview
                }))
            .Take(2)
            .ToList();

        // Show only an unambiguous Contact item.
        if (contacts.Count != 1)
        {
            return null;
        }

        var contact = contacts[0];

        return new ContactViewModel
        {
            PhoneNumber = contact.ContactPhone ?? string.Empty,
            PhoneNumber2 = contact.ContactPhone2 ?? string.Empty,
            DisplayAddress = contact.ContactDisplayAddress ?? string.Empty
        };
    }
}

public sealed class FAQsPageViewModel
{
    public string Heading { get; init; } = "FAQs";
    public string Content { get; init; } = string.Empty;
    public string JsonSchema { get; init; } = string.Empty;

    public GenericSideContactViewModel ContactDetails { get; init; }
        = new();
}

public sealed class GenericSideContactViewModel
{
    public ContactViewModel? GeneralContact { get; init; }
}

public sealed class ContactViewModel
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string PhoneNumber2 { get; init; } = string.Empty;
    public string DisplayAddress { get; init; } = string.Empty;
}