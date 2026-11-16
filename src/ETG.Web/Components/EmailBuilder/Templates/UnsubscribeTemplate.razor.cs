using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: UnsubscribeTemplate.IDENTIFIER,
    name: "Unsubscribe Template",
    componentType: typeof(ETG.Web.Components.EmailBuilder.Templates.UnsubscribeTemplate),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.Lock)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class UnsubscribeTemplate : ComponentBase
{
    public const string IDENTIFIER = "ETG.UnsubscribeTemplate";

    [Parameter]
    public EmailTemplate Model { get; set; } = default!;
}
