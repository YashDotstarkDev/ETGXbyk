using System.Threading.Tasks;
using CMS.EmailEngine;
using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: ETGMasterEmailTemplateNewAddress.IDENTIFIER,
    name: "ETG Master Template (New Address)",
    componentType: typeof(ETGMasterEmailTemplateNewAddress),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.Messages)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class ETGMasterEmailTemplateNewAddress : ComponentBase
{
    public const string IDENTIFIER = "ETG.EmailTemplate.Master.NewAddress";

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