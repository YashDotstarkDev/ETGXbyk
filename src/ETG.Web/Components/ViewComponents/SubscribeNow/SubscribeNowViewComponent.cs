using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

    // Optional: supplied by the page, omitted by the footer.
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

public static class GetSubscribeNowQuery
{
    public static SubscribeNowViewModel Get()
    {
        return new SubscribeNowViewModel();
    }

    public static bool Save(SubscribeNowViewModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                validationResults,
                validateAllProperties: true))
        {
            return false;
        }

        var email = model.EmailAddress.Trim();

        var consumers = BizFormItemProvider
            .GetItems<CompetitionItem>()
            .WhereEquals(
                nameof(CompetitionItem.EmailAddress),
                email)
            .ToList();

        var agents = BizFormItemProvider
            .GetItems<CompetitionAgentsItem>()
            .WhereEquals(
                nameof(CompetitionAgentsItem.EmailAddress),
                email)
            .ToList();

        // Check existing records across both forms.
        var previouslySubscribed =
            consumers.Count > 0 || agents.Count > 0;

        if (model.IsTravelAgent == "Yes")
        {
            // Preserve existing duplicate-selection behaviour.
            var agent = agents
                .OrderBy(x => x.ItemID)
                .FirstOrDefault();

            if (agent == null)
            {
                agent = new CompetitionAgentsItem
                {
                    FirstName = string.Empty,
                    LastName = string.Empty
                };
            }

            agent.EmailAddress = email;
            agent.HasPreviouslySubscribed = previouslySubscribed;

            // Preserve existing names when the footer omits them.
            if (!string.IsNullOrWhiteSpace(model.FirstName))
            {
                agent.FirstName = model.FirstName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.LastName))
            {
                agent.LastName = model.LastName.Trim();
            }

            // Update the system modification timestamp.
            // Do not overwrite the original FormInserted value.
            agent.FormUpdated = DateTime.Now;

            BizFormItemProvider.SetItem(agent);

            return agent.ItemID > 0;
        }

        // Preserve existing duplicate-selection behaviour.
        var consumer = consumers
            .OrderBy(x => x.ItemID)
            .FirstOrDefault();

        if (consumer == null)
        {
            consumer = new CompetitionItem
            {
                FirstName = string.Empty,
                LastName = string.Empty
            };
        }

        consumer.EmailAddress = email;

        consumer.SetValue(
            nameof(CompetitionItem.IsTravelAgent),
            false);

        consumer.SetValue(
            nameof(CompetitionItem.HasPreviouslySubscribed),
            previouslySubscribed);

        // Preserve existing names when the footer omits them.
        if (!string.IsNullOrWhiteSpace(model.FirstName))
        {
            consumer.FirstName = model.FirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.LastName))
        {
            consumer.LastName = model.LastName.Trim();
        }

        // Update the system modification timestamp.
        // Do not overwrite the original FormInserted value.
        consumer.FormUpdated = DateTime.Now;

        BizFormItemProvider.SetItem(consumer);

        return consumer.ItemID > 0;
    }
}