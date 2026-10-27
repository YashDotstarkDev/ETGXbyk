using System.ComponentModel.DataAnnotations;
using TourContent = ETG.Core.Content.Tour;

namespace ETG.Core.Models.Tour;

public sealed class TourEnquireViewModel
{
    public TourContent Tour { get; set; } = default!;

    public TourEnquireFormViewModel Form { get; set; } = new();

    public ShareLinksViewModel? ShareLinks { get; set; }
}

public sealed class TourEnquireFormViewModel
{
    [Required(ErrorMessage = "Please enter a valid first name.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Please enter a valid last name.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Email must be a valid e-mail")]
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Please select a preferred contact method.")]
    public string PreferredContactMethod { get; set; } = "Email";
    public string Message { get; set; } = string.Empty;
    public bool SubscribeToNewsletter { get; set; }
    public string TourCode { get; set; } = string.Empty;
    public string TourLink { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public string TourClass { get; set; } = string.Empty;
    public string TourDepartureCity { get; set; } = string.Empty;
    public string TourQuotedPrice { get; set; } = string.Empty;
    public string PriceCurrency { get; set; } = string.Empty;
    public string TourDiscountText { get; set; } = string.Empty;
    public string TourDate { get; set; } = string.Empty;
    public string DepartureDate { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string PreferredDestination { get; set; } = string.Empty;
}