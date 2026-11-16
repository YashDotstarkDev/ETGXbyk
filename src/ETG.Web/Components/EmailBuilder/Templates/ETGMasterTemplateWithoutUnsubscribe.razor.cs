using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: ETGMasterTemplateWithoutUnsubscribe.IDENTIFIER,
    name: "ETG Master Template (Without Unsubscribe)",
    componentType: typeof(ETGMasterTemplateWithoutUnsubscribe),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.Newspaper)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class ETGMasterTemplateWithoutUnsubscribe : ComponentBase
{
    public const string IDENTIFIER = "ETG.EmailTemplate.Master.WithoutUnsubscribe";

    [Parameter]
    public EmailTemplate Model { get; set; } = default!;
}
