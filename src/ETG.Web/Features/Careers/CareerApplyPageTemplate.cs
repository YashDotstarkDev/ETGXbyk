using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    contentTypeName: Page.CONTENT_TYPE_NAME,
    controllerType: typeof(ETG.Web.Features.Careers.CareerApplyPageTemplateController),
    Path = "/careers/apply",
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Careers;

public sealed class CareerApplyPageTemplateProperties : IPageTemplateProperties
{
}

public sealed class CareerApplyPageTemplateController(
    IWebPageDataContextRetriever contextRetriever,
    IWebPageDataContextInitializer contextInitializer,
    IContentQueryExecutor queryExecutor,
    IWebsiteChannelContext channelContext,
    ILogger<CareerApplyPageTemplateController> logger,
    WebPageMetaService metaService) : Controller
{
    [HttpGet("~/careers/apply")]
    public async Task<IActionResult> Index()
    {
        return await HandleCareerApply(null);
    }

    [HttpGet("~/careers/{roleAlias}/apply")]
    public async Task<IActionResult> ApplyForRole(string roleAlias)
    {
        return await HandleCareerApply(roleAlias);
    }

    private async Task<IActionResult> HandleCareerApply(string? roleAlias)
    {
        try
        {
            Page? page = null;
            bool hasWebPageContext = false;

            if (contextRetriever.TryRetrieve(out var data) && data?.WebPage != null)
            {
                hasWebPageContext = true;
                var query = new ContentItemQueryBuilder()
                    .ForContentType(
                        Page.CONTENT_TYPE_NAME,
                        config => config
                            .ForWebsite(channelContext.WebsiteChannelName)
                            .WithLinkedItems(1)
                            .Where(w => w.WhereEquals("WebPageItemID", data.WebPage.WebPageItemID)));

                var result = await queryExecutor.GetMappedWebPageResult<Page>(
                    query,
                    cancellationToken: HttpContext.RequestAborted);
                page = result?.FirstOrDefault();
            }

            if (page == null)
            {
                var query = new ContentItemQueryBuilder()
                    .ForContentType(
                        Page.CONTENT_TYPE_NAME,
                        config => config
                            .ForWebsite(channelContext.WebsiteChannelName, PathMatch.Single("/careers/apply"))
                            .WithLinkedItems(1));

                var result = await queryExecutor.GetMappedWebPageResult<Page>(
                    query,
                    cancellationToken: HttpContext.RequestAborted);
                page = result?.FirstOrDefault();

                if (page != null)
                {
                    var routedPage = new RoutedWebPage
                    {
                        WebPageItemID = page.SystemFields.WebPageItemID,
                        WebPageItemGUID = page.SystemFields.WebPageItemGUID,
                        LanguageID = page.SystemFields.ContentItemCommonDataContentLanguageID,
                        LanguageName = "en",
                        ContentTypeName = Page.CONTENT_TYPE_NAME,
                        WebsiteChannelName = !string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName)
                            ? channelContext.WebsiteChannelName
                            : "ETG"
                    };

                    contextInitializer.Initialize(routedPage);
                }
            }

            if (page == null)
            {
                logger.LogWarning("Unable to retrieve Career Apply page context.");
                return NotFound();
            }

            var roleTitle = Request.Query["role"].ToString();
            if (string.IsNullOrWhiteSpace(roleTitle))
            {
                if (string.IsNullOrWhiteSpace(roleAlias) && RouteData.Values.TryGetValue("roleAlias", out var roleAliasObj))
                {
                    roleAlias = roleAliasObj?.ToString();
                }

                if (!string.IsNullOrWhiteSpace(roleAlias))
                {
                    try
                    {
                        var rolesQuery = new ContentItemQueryBuilder()
                            .ForContentType(
                                CareerRole.CONTENT_TYPE_NAME,
                                config => config.ForWebsite(channelContext.WebsiteChannelName));

                        var roles = await queryExecutor.GetMappedWebPageResult<CareerRole>(
                            rolesQuery,
                            cancellationToken: HttpContext.RequestAborted);

                        var matchedRole = roles?.FirstOrDefault(r =>
                            string.Equals(
                                r.SystemFields.WebPageItemTreePath?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(),
                                roleAlias,
                                StringComparison.OrdinalIgnoreCase)
                            || string.Equals(r.SystemFields.WebPageItemName, roleAlias, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(r.CareerRoleTitle?.Replace(" ", "-"), roleAlias, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(r.DocumentName?.Replace(" ", "-"), roleAlias, StringComparison.OrdinalIgnoreCase));

                        if (matchedRole != null)
                        {
                            roleTitle = !string.IsNullOrWhiteSpace(matchedRole.CareerRoleTitle)
                                ? matchedRole.CareerRoleTitle
                                : matchedRole.DocumentName ?? matchedRole.SystemFields.WebPageItemName;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to resolve CareerRole by alias {RoleAlias}", roleAlias);
                    }

                    if (string.IsNullOrWhiteSpace(roleTitle))
                    {
                        roleTitle = roleAlias.Replace("-", " ");
                    }
                }
            }

            var viewModel = new CareerApplyPageViewModel
            {
                Page = page,
                HasWebPageContext = hasWebPageContext,
                Role = !string.IsNullOrWhiteSpace(roleTitle)
                    ? new CareerRoleInfoViewModel { Title = roleTitle.Trim() }
                    : null,
                Form = new CareerApplyFormViewModel
                {
                    Role = roleTitle?.Trim() ?? string.Empty,
                    PreferredContactMethod = "Email"
                }
            };

            if (page is IMetaDataSchema metaFields)
            {
                var heroImage = page.PageHeroImage?.OfType<ETG.Core.Content.MediaFile>().FirstOrDefault();
                metaService.SetMeta(metaFields, heroImage?.LegacyMediaFileAsset?.Url);
            }

            return View("~/Features/Careers/CareerApplyPage.cshtml", viewModel);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading Career Apply page.");
            return StatusCode(500);
        }
    }
}


public sealed class CareerApplyPageViewModel
{
    public Page Page { get; set; } = default!;
    public CareerRoleInfoViewModel? Role { get; set; }
    public CareerApplyFormViewModel Form { get; set; } = new();
    public bool HasWebPageContext { get; set; }
}

public sealed class CareerRoleInfoViewModel
{
    public string Title { get; set; } = string.Empty;
}

public sealed class CareerApplyFormViewModel
{
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PreferredContactMethod { get; set; } = "Email";
    public string Comments { get; set; } = string.Empty;
    public IFormFile? CVFile { get; set; }

    public string Friend { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
