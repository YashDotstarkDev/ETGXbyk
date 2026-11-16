using CMS.EmailMarketing.Internal;

namespace ETG.Web.Services.Email;

public sealed class FormEmailDataContext : FormAutoresponderEmailDataContext
{
    public Dictionary<string, string> Items { get; set; } = new();
}