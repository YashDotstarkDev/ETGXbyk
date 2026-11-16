using System.Threading.Tasks;
using CMS.EmailEngine;
using ETG;
using ETG.Web.Components.EmailBuilder.Templates;
using Kentico.EmailBuilder.Web.Mvc;
using Microsoft.AspNetCore.Components;
using Icons = Kentico.Xperience.Admin.Base.Icons;

[assembly: RegisterEmailTemplate(
    identifier: SubscriptionTemplate.IDENTIFIER,
    name: "Subscription Template",
    componentType: typeof(ETG.Web.Components.EmailBuilder.Templates.SubscriptionTemplate),
    ContentTypeNames = [EmailTemplate.CONTENT_TYPE_NAME],
    IconClass = Icons.ShoppingCart)]

namespace ETG.Web.Components.EmailBuilder.Templates;

public partial class SubscriptionTemplate : ComponentBase
{
    public const string IDENTIFIER = "ETG.SubscriptionTemplate";

    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; } = default!;

    public EmailTemplate? Model { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        Model = await context.GetEmail<EmailTemplate>();
    }
}