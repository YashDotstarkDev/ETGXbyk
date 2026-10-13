using ETG.Web.Components.PageBuilder.Sections.Form;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterSection(
    identifier: FormSection.IDENTIFIER,
    viewComponentType: typeof(FormSection),
    name: "Form",
    propertiesType: typeof(FormSectionProperties),
    Description = "Form Section to be used at the top of a page",
    IconClass = KenticoIcons.FORM
)]

namespace ETG.Web.Components.PageBuilder.Sections.Form;

public class FormSection : ViewComponent
{
    public const string IDENTIFIER = "ETG.Sections.FormSection";

    public IViewComponentResult Invoke() => View("~/Components/PageBuilder/Sections/Form/FormSection.cshtml");
}

public class FormSectionProperties : ISectionProperties
{

}