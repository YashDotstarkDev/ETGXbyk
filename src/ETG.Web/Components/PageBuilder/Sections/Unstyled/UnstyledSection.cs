using ETG.Web.Components.PageBuilder.Sections.Unstyled;
using Kentico.PageBuilder.Web.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterSection(
    identifier: UnstyledSection.IDENTIFIER,
    name: "Unstyled",
    propertiesType: null,
    customViewName: "~/Components/PageBuilder/Sections/Unstyled/Unstyled.cshtml",
    Description = "A completely unstyled section with a single Widget Zone. Can be used as a placeholder section.",
    IconClass = KenticoIcons.RECTANGLE_O_H
)]

namespace ETG.Web.Components.PageBuilder.Sections.Unstyled;

public class UnstyledSection
{
    public const string IDENTIFIER = "Website.Sections.Unstyled";
}
