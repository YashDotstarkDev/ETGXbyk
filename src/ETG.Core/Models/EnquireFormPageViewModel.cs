using ETG.Core.Content;
using ETG.Core.Content.Pages;
namespace ETG.Core.Models;

public sealed class EnquireFormPageViewModel
{
    public Container Page { get; set; } = default!;
    public EnquireFormViewModel Form { get; set; } = new();
    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>Destinations { get; set; } = [];
    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>Experiences  { get; set; } = [];
}

public sealed class EnquireFormViewModel
{
    public string Reason { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PreferredContactType { get; set; } = "Email";
    public string PreferredTourType { get; set; } = string.Empty;
    public string PreferredDestination { get; set; } = string.Empty;
    public string PreferredExperience { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool SubscribeToNewsletter { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
