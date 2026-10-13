using ETG.Web.Components.PageBuilder.Sections.Table;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterSection(
    identifier: TableSection.IDENTIFIER,
    viewComponentType: typeof(TableSection),
    name: "Table",
    propertiesType: typeof(TableSectionProperties),
    Description = "Table Section",
    IconClass = KenticoIcons.TABLE
)]

namespace ETG.Web.Components.PageBuilder.Sections.Table;

public class TableSection : ViewComponent
{
    public const string IDENTIFIER = "ETG.Sections.TableSection";

    public IViewComponentResult Invoke() => View("~/Components/PageBuilder/Sections/Table/TableSection.cshtml");
}

public class TableSectionProperties : ISectionProperties
{

}