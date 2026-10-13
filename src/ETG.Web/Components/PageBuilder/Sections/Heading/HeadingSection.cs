using ETG.Web.Components.PageBuilder.Sections.Heading;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterSection(
    identifier: HeadingSection.IDENTIFIER,
    viewComponentType: typeof(HeadingSection),
    name: "Heading",
    propertiesType: typeof(HeadingSectionProperties),
    Description = "Heading Section to be used at the top of a page",
    IconClass = KenticoIcons.I
)]

namespace ETG.Web.Components.PageBuilder.Sections.Heading;

public class HeadingSection : ViewComponent
{
    public const string IDENTIFIER = "Website.Sections.Heading";

    public IViewComponentResult Invoke() => View("~/Components/PageBuilder/Sections/Heading/HeadingSection.cshtml");
}

public class HeadingSectionProperties : ISectionProperties
{

}
