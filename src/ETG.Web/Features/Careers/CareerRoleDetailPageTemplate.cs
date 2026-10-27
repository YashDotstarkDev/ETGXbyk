using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Models;
using ETG.Web.Features;
using ETG.Web.Features.Career.Operations;
using ETG.Web.Infrastructure;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(
    contentTypeName: CareerRole.CONTENT_TYPE_NAME,
    controllerType: typeof(
        ETG.Web.Features.Career.CareerRoleDetailPageTemplateController),
    WebsiteChannelNames = new[] { "ETG" }
)]

namespace ETG.Web.Features.Career;

public class CareerRoleDetailPageTemplateProperties : IPageTemplateProperties
{
}

public class CareerRoleDetailPageTemplateController(
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
            new CareerRoleDetailPageQuery(
                data.WebPage,
                channelContext.WebsiteChannelName
            )
        );

        if (result?.Role is null)
        {
            return NotFound();
        }

        var role = result.Role;
        var url = await urlRetriever.Retrieve(role);

        var title = !string.IsNullOrWhiteSpace(role.CareerRoleTitle)
            ? role.CareerRoleTitle
            : role.DocumentName ?? role.SystemFields.WebPageItemName ?? "";

        // Extract URL alias slug (prioritizing tree path segment to match old Kentico 13 NodeAlias)
        var pageAlias = role.SystemFields.WebPageItemTreePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault()
            ?? role.SystemFields.WebPageItemName;

        var viewModel = new CareerRoleDetailPageViewModel
        {
            BasicInfo = new CareerRoleBasicInfoViewModel
            {
                Title = title,
                Location = role.CareerRoleLocation ?? "",
                PositionType = role.CareerRolePositionType ?? "",
                Path = url.RelativePath
            },
            Title = title,
            Description = role.CareerRoleDescription ?? "",
            SummaryOfPosition = role.CareerRoleSummaryOfPosition ?? "",
            DutiesAndResponsibilities = role.CareerRoleDutiesAndResponsibilities ?? "",
            Address = role.CareerRoleAddress ?? "",
            State = role.CareerRoleState ?? "",
            PageAlias = pageAlias,
            ApplyUrl = !string.IsNullOrWhiteSpace(title)
                ? $"/careers/apply?role={Uri.EscapeDataString(title)}"
                : "/careers/apply"
        };

        if (role is IMetaDataSchema metaFields)
        {
            metaService.SetMeta(metaFields);
        }

        return View(
           "~/Features/Careers/CareerRoleDetailPage_Default.cshtml",
           viewModel);

    }
}

public class CareerRoleDetailPageViewModel
{
    public CareerRoleBasicInfoViewModel BasicInfo { get; init; } = new();
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string SummaryOfPosition { get; init; } = "";
    public string DutiesAndResponsibilities { get; init; } = "";
    public string Address { get; init; } = "";
    public string State { get; init; } = "";
    public string PageAlias { get; init; } = "";
    public string ApplyUrl { get; init; } = "";
}

public class CareerRoleBasicInfoViewModel
{
    public string Title { get; init; } = "";
    public string Location { get; init; } = "";
    public string PositionType { get; init; } = "";
    public string Path { get; init; } = "";
}
