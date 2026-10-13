using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CMS.OnlineForms;
using ETG.Core.Content.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Components.ViewComponents.SubscribeNow;

public class SubscribeNowViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View(
            "~/Components/ViewComponents/SubscribeNow/SubscribeNow.cshtml",
            GetSubscribeNowQuery.Get());
    }
}

public class SubscribeNowViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254, ErrorMessage = "Email address is too long.")]
    [RegularExpression(
        @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)+$",
        ErrorMessage = "Please enter a valid email address.")]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an option.")]
    [RegularExpression(
        "^(Yes|No)$",
        ErrorMessage = "Please select Yes or No.")]
    public string IsTravelAgent { get; set; } = string.Empty;
}

public static class GetSubscribeNowQuery
{
    public static SubscribeNowViewModel Get()
    {
        return new SubscribeNowViewModel();
    }

    public static bool Save(SubscribeNowViewModel model)
    {
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true))
        {
            return false;
        }

        var competitionItem = new CompetitionItem
        {
            EmailAddress = model.EmailAddress.Trim(),
            IsTravelAgent = model.IsTravelAgent,

            // Not collected by this subscription form.
            FirstName = string.Empty,
            LastName = string.Empty,
            HasPreviouslySubscribed = string.Empty
        };

        BizFormItemProvider.SetItem(competitionItem);

        return competitionItem.ItemID > 0;
    }
}