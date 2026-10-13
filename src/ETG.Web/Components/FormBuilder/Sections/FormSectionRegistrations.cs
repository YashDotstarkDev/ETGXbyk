using Kentico.Forms.Web.Mvc;

/// <summary>
/// Registers custom form sections for the Form Builder.
/// </summary>
[assembly: RegisterFormSection(
    "XByK.FormSections.FiftyFifty",
    "50/50 Layout",
    customViewName: "~/Components/FormBuilder/Sections/_FiftyFifty.cshtml",
    Description = "Organizes form fields into two equal-width columns side-by-side.",
    IconClass = "icon-l-cols-2")]

[assembly: RegisterFormSection(
    "XByK.FormSections.FullWidth",
    "Full Width Layout",
    customViewName: "~/Components/FormBuilder/Sections/_FullWidth.cshtml",
    Description = "Organizes form fields in a full-width single column layout.",
    IconClass = "icon-square")]

namespace ETG.Web.Components.FormBuilder.Sections;