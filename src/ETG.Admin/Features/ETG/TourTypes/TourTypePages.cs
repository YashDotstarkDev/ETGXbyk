using CMS.DataEngine;
using CMS.Membership;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ETGApplicationPage),
    slug: "tour-types",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypesListPage),
    name: "Tour Types",
    templateName: TemplateNames.LISTING,
    order: 3)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypesListPage),
    slug: "create",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypeCreatePage),
    name: "New Tour Type",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypesListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypeEditSection),
    name: "Edit Tour Type",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypeEditSection),
    slug: "edit",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.TourTypes.TourTypeEditPage),
    name: "Edit Tour Type",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETG.TourTypes;

/// <summary>
/// Stores the exact code name of the UI form configured
/// under the Tour Type module class.
/// </summary>
internal static class TourTypePageSettings
{
    public const string FormName = "tourtypeadminedit";
}

/// <summary>
/// Displays all Tour Type records.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class TourTypesListPage : ListingPage
{
    protected override string ObjectType =>
        TourTypeInfo.OBJECT_TYPE;

    public override Task ConfigurePage()
    {
        PageConfiguration.HeaderActions
            .AddLink<TourTypeCreatePage>(
                "New Tour Type");

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                "TourTypeID",
                "ID",
                visible: false,
                minWidth: 0,
                maxWidth: 0,
                searchable: false,
                localizable: false,
                sortable: true)
            .AddColumn(
                "TourTypeName",
                "Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true)
            .AddColumn(
                "TourTypeCodeName",
                "Code Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true)
            .AddColumn(
                "TourTypeIcon",
                "Icon",
                visible: true,
                minWidth: 180,
                maxWidth: 500,
                searchable: false,
                localizable: false,
                sortable: false)
            .AddColumn(
                "TourTypeWhiteIconImage",
                "White Icon Image",
                visible: true,
                minWidth: 250,
                maxWidth: 600,
                searchable: false,
                localizable: false,
                sortable: false)
            .AddColumn(
                "TourTypeBlackIconImage",
                "Black Icon Image",
                visible: true,
                minWidth: 250,
                maxWidth: 600,
                searchable: false,
                localizable: false,
                sortable: false)
            .AddColumn(
                "TourTypeDescription",
                "Description",
                visible: true,
                minWidth: 300,
                maxWidth: 700,
                searchable: false,
                localizable: false,
                sortable: false)
            .AddColumn(
                "TourTypeUrl",
                "URL",
                visible: true,
                minWidth: 250,
                maxWidth: 600,
                searchable: false,
                localizable: false,
                sortable: false);

        PageConfiguration
            .AddEditRowAction<TourTypeEditSection>();

        PageConfiguration.TableActions
            .AddDeleteAction(
                nameof(DeleteTourType),
                "Delete");

        PageConfiguration.QueryModifiers.AddModifier(
            (query, _) => query.Columns(
                "TourTypeID",
                "TourTypeName",
                "TourTypeCodeName",
                "TourTypeIcon",
                "TourTypeWhiteIconImage",
                "TourTypeBlackIconImage",
                "TourTypeDescription",
                "TourTypeUrl"));

        return base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> DeleteTourType(
        int id,
        CancellationToken cancellationToken)
    {
        var tourType =
            await Provider<TourTypeInfo>
                .Instance
                .GetAsync(id, cancellationToken);

        if (tourType is null)
        {
            return ResponseFrom(new CommandResponse())
                .AddErrorMessage(
                    "Tour Type not found.");
        }

        tourType.Delete();

        return ResponseFrom(new CommandResponse())
            .AddSuccessMessage(
                "Tour Type deleted successfully.");
    }
}

/// <summary>
/// Creates a new Tour Type record using the
/// 'tourtypeadminedit' UI form.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class TourTypeCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : CreatePage<TourTypeInfo, TourTypeEditSection>(
        formComponentMapper,
        formDataBinder,
        pageLinkGenerator)
{
    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            TourTypePageSettings.FormName;

        return base.ConfigurePage();
    }
}

/// <summary>
/// Parameterized section page that receives the
/// Tour Type record ID from the URL.
/// </summary>
public class TourTypeEditSection
    : EditSectionPage<TourTypeInfo>
{
}

/// <summary>
/// Edits an existing Tour Type record using the
/// 'tourtypeadminedit' UI form.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class TourTypeEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder)
    : InfoEditPage<TourTypeInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(TourTypeEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            TourTypePageSettings.FormName;

        return base.ConfigurePage();
    }
}