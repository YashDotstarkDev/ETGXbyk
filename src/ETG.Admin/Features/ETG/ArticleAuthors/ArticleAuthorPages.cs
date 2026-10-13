using CMS.DataEngine;
using CMS.Membership;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ETGApplicationPage),
    slug: "article-authors",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorsListPage),
    name: "Article Authors",
    templateName: TemplateNames.LISTING,
    order: 1)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorsListPage),
    slug: "create",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorCreatePage),
    name: "CREATE",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorsListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorEditSection),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorEditSection),
    slug: "edit",
    uiPageType: typeof(
        ETG.Admin.Features.ETG.ArticleAuthors.ArticleAuthorEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETG.ArticleAuthors;

internal static class ArticleAuthorPageSettings
{
    public const string FormName = "articleauthoredit";
}

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ArticleAuthorsListPage : ListingPage
{
    protected override string ObjectType => ArticleAuthorInfo.OBJECT_TYPE;

    public override Task ConfigurePage()
    {
        PageConfiguration.HeaderActions
            .AddLink<ArticleAuthorCreatePage>(
                "New Article Author");

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                "ArticleAuthorID",
                "ID",
                visible: false,
                minWidth: 0,
                maxWidth: 0,
                searchable: false,
                localizable: false,
                sortable: true)
            .AddColumn(
                "ArticleAuthorFullName",
                "Full Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true)
            .AddColumn(
                "ArticleAuthorCodeName",
                "Code Name",
                visible: true,
                minWidth: 200,
                maxWidth: 500,
                searchable: true,
                localizable: false,
                sortable: true);

        PageConfiguration
            .AddEditRowAction<ArticleAuthorEditSection>();

        PageConfiguration.TableActions
            .AddDeleteAction(
                nameof(DeleteArticleAuthor),
                "Delete");

        PageConfiguration.QueryModifiers.AddModifier(
            (query, _) => query.Columns(
                "ArticleAuthorID",
                "ArticleAuthorFullName",
                "ArticleAuthorCodeName"));

        return base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> DeleteArticleAuthor(
        int id,
        CancellationToken cancellationToken)
    {
        var articleAuthor =
            await Provider<ArticleAuthorInfo>
                .Instance
                .GetAsync(id, cancellationToken);

        if (articleAuthor is null)
        {
            return ResponseFrom(new CommandResponse())
                .AddErrorMessage(
                    "Article Author not found.");
        }

        articleAuthor.Delete();

        return ResponseFrom(new CommandResponse())
            .AddSuccessMessage(
                "Article Author deleted successfully.");
    }
}

[UIEvaluatePermission(SystemPermissions.CREATE)]
public class ArticleAuthorCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : CreatePage<
        ArticleAuthorInfo,
        ArticleAuthorEditSection>(
            formComponentMapper,
            formDataBinder,
            pageLinkGenerator)
{
    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ArticleAuthorPageSettings.FormName;

        return base.ConfigurePage();
    }
}

public class ArticleAuthorEditSection
    : EditSectionPage<ArticleAuthorInfo>
{
}

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ArticleAuthorEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder)
    : InfoEditPage<ArticleAuthorInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(ArticleAuthorEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ArticleAuthorPageSettings.FormName;

        return base.ConfigurePage();
    }
}