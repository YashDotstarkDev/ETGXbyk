using System.Threading.Tasks;
using CMS.EmailEngine;
using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: ETGMasterTemplate2024.IDENTIFIER,
    name: "ETG Master Template (2024)",
    componentType: typeof(ETGMasterTemplate2024),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.Calendar)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class ETGMasterTemplate2024 : ComponentBase
{
    public const string IDENTIFIER = "ETG.EmailTemplate.Master2024";

    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; } = default!;

    /// <summary>
    /// Email content item selected in the Email Channel.
    /// </summary>
    public EmailTemplate? Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        Model = await context.GetEmail<EmailTemplate>();
    }
}