using System.Threading.Tasks;
using CMS.EmailEngine;
using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: ETGMasterEmailTemplate.IDENTIFIER,
    name: "ETG Master Template",
    componentType: typeof(ETGMasterEmailTemplate),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.Message)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class ETGMasterEmailTemplate : ComponentBase
{
    public const string IDENTIFIER = "ETG.EmailTemplate.Master";

    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; } = default!;

    public EmailTemplate? Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        Model = await context.GetEmail<EmailTemplate>();
    }
}