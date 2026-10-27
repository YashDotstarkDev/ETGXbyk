using ETG.Core.Content;
using ETG.Core.Content.Pages;
using System.Text.RegularExpressions;
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
    public string DepartureDate { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool SubscribeToNewsletter { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            ErrorMessage = "Please enter valid values.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName) ||
            (PreferredContactType == "Email" && string.IsNullOrWhiteSpace(Email)) ||
            (PreferredContactType == "Phone" && string.IsNullOrWhiteSpace(Phone)))
        {
            ErrorMessage = "Please enter values to mandatory fields.";
            return false;
        }

        if (FirstName.Equals(LastName, StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "First and last name cannot be the same";
            return false;
        }

        Regex regex = new Regex("^[0-9]+$");

        if (FirstName.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            LastName.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(Phone) && !regex.IsMatch(Phone.Trim())))
        {
            ErrorMessage = "Please enter valid values.";
            return false;
        }

        return true;
    }
}
