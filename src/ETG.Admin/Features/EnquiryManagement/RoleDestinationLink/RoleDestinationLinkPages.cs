using CMS.DataEngine;
using CMS.Membership;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.EnquiryManagement.EnquiryManagementApplicationPage),
    slug: "role-destination-links",
    uiPageType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinksListPage),
    name: "Role to Destination Links",
    templateName: TemplateNames.LISTING,
    order: 1)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinksListPage),
    slug: "create",
    uiPageType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinkCreatePage),
    name: "Create",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinksListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinkEditSection),
    name: "Edit ",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinkEditSection),
    slug: "edit",
    uiPageType: typeof(
        ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks
            .RoleDestinationLinkEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.EnquiryManagement.RoleDestinationLinks;

internal static class RoleDestinationLinkPageSettings
{
    public const string FormName = "roledestinationlinkedit";
}

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class RoleDestinationLinksListPage : ListingPage
{
    protected override string ObjectType =>
        RoleToDestinationLinkInfo.OBJECT_TYPE;

    public override Task ConfigurePage()
    {
        PageConfiguration.HeaderActions
            .AddLink<RoleDestinationLinkCreatePage>(
                "New Role to Destination Link");

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                nameof(RoleToDestinationLinkInfo.RoleDestinationLinkID),
                "ID",
                visible: false,
                searchable: false,
                localizable: false,
                sortable: true)
            .AddColumn(
                nameof(RoleToDestinationLinkInfo.RoleDestinationName),
                "Name",
                visible: true,
                searchable: true,
                localizable: false,
                sortable: true)
            .AddColumn(
                nameof(RoleToDestinationLinkInfo.RoleDestinationDestinations),
                "Destinations",
                visible: true,
                searchable: true,
                localizable: false,
                sortable: false);

        PageConfiguration
            .AddEditRowAction<RoleDestinationLinkEditSection>();

        PageConfiguration.TableActions
            .AddDeleteAction(nameof(DeleteLink), "Delete");

        PageConfiguration.QueryModifiers.AddModifier((query, _) =>
            query.Columns(
                nameof(RoleToDestinationLinkInfo.RoleDestinationLinkID),
                nameof(RoleToDestinationLinkInfo.RoleDestinationName),
                nameof(RoleToDestinationLinkInfo.RoleDestinationDestinations)));

        return base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> DeleteLink(
        int id,
        CancellationToken cancellationToken)
    {
        var item = await Provider<RoleToDestinationLinkInfo>
            .Instance
            .GetAsync(id, cancellationToken);

        if (item is null)
        {
            return ResponseFrom(new CommandResponse())
                .AddErrorMessage(
                    "Role to Destination Link not found.");
        }

        item.Delete();

        return ResponseFrom(new CommandResponse())
            .AddSuccessMessage(
                "Role to Destination Link deleted.");
    }
}

[UIEvaluatePermission(SystemPermissions.CREATE)]
public class RoleDestinationLinkCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : CreatePage<
        RoleToDestinationLinkInfo,
        RoleDestinationLinkEditSection>(
            formComponentMapper,
            formDataBinder,
            pageLinkGenerator)
{
    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            RoleDestinationLinkPageSettings.FormName;

        return base.ConfigurePage();
    }
}

public class RoleDestinationLinkEditSection
    : EditSectionPage<RoleToDestinationLinkInfo>
{
}

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class RoleDestinationLinkEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder)
    : InfoEditPage<RoleToDestinationLinkInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(RoleDestinationLinkEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            RoleDestinationLinkPageSettings.FormName;

        return base.ConfigurePage();
    }
}