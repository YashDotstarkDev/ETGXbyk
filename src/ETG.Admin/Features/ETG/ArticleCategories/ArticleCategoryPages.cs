using CMS.DataEngine;
using CMS.Membership;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ETGApplicationPage),
    slug: "article-categories",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoriesListPage),
    name: "Article Categories",
    templateName: TemplateNames.LISTING,
    order: 2)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoriesListPage),
    slug: "create",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoryCreatePage),
    name: "CREATE",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoriesListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoryEditSection),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoryEditSection),
    slug: "edit",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleCategories.ArticleCategoryEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETG.ArticleCategories;

internal static class ArticleCategoryPageSettings
{
    public const string FormName =
        "articlecategoryedit";
}

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ArticleCategoriesListPage : ListingPage
{
    protected override string ObjectType =>
        ArticleCategoryInfo.OBJECT_TYPE;

    public override Task ConfigurePage()
    {
        PageConfiguration.HeaderActions
            .AddLink<ArticleCategoryCreatePage>(
                "New Article Category");

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                "ArticleCategoryID",
                "ID",
                visible: false,
                minWidth: 0,
                maxWidth: 0,
                searchable: false,
                localizable: false,
                sortable: true)
            .AddColumn(
                "ArticleCategoryName",
                "Category Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true)
            .AddColumn(
                "ArticleCategoryCodeName",
                "Code Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true);

        PageConfiguration
            .AddEditRowAction<ArticleCategoryEditSection>();

        PageConfiguration.TableActions
            .AddDeleteAction(
                nameof(DeleteArticleCategory),
                "Delete");

        PageConfiguration.QueryModifiers.AddModifier(
            (query, _) => query.Columns(
                "ArticleCategoryID",
                "ArticleCategoryName",
                "ArticleCategoryCodeName"));

        return base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> DeleteArticleCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var articleCategory =
            await Provider<ArticleCategoryInfo>
                .Instance
                .GetAsync(id, cancellationToken);

        if (articleCategory is null)
        {
            return ResponseFrom(new CommandResponse())
                .AddErrorMessage(
                    "Article Category not found.");
        }

        articleCategory.Delete();

        return ResponseFrom(new CommandResponse())
            .AddSuccessMessage(
                "Article Category deleted successfully.");
    }
}

[UIEvaluatePermission(SystemPermissions.CREATE)]
public class ArticleCategoryCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : CreatePage<
        ArticleCategoryInfo,
        ArticleCategoryEditSection>(
            formComponentMapper,
            formDataBinder,
            pageLinkGenerator)
{
    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ArticleCategoryPageSettings.FormName;

        return base.ConfigurePage();
    }
}

public class ArticleCategoryEditSection
    : EditSectionPage<ArticleCategoryInfo>
{
}

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ArticleCategoryEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder)
    : InfoEditPage<ArticleCategoryInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(ArticleCategoryEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ArticleCategoryPageSettings.FormName;

        return base.ConfigurePage();
    }
}