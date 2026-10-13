using CMS.ContentEngine;
using ETG;
using ETG.Core.Content;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.GenericEnquireSidebar;

public class GenericEnquireSidebarViewComponent : ViewComponent
{
    private readonly IContentQueryExecutor contentQueryExecutor;

    public GenericEnquireSidebarViewComponent(
        IContentQueryExecutor contentQueryExecutor)
    {
        this.contentQueryExecutor = contentQueryExecutor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                EnquiryContact.CONTENT_TYPE_NAME);

        var enquiryContacts =
            await contentQueryExecutor.GetMappedResult<EnquiryContact>(query);

        var contacts = enquiryContacts
            .Where(x => x != null)
            .ToList();

        if (contacts.Count == 0)
        {
            return Content(string.Empty);
        }

        var model = new GenericEnquireSidebarViewModel
        {
            EnquiryContacts = contacts
        };

        return View(
            "~/Components/ViewComponents/GenericEnquireSidebar/GenericEnquireSidebar.cshtml",
            model);
    }
}

public class GenericEnquireSidebarViewModel
{
    public IReadOnlyList<EnquiryContact> EnquiryContacts { get; set; }
        = [];
}