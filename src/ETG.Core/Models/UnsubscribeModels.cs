using ETG.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ETG.Web.Models;

public sealed class UnsubscribeInputModel
{
    [Required(ErrorMessage = "Please select a reason.")]
    public Guid? ReasonGuid { get; set; }

    [Required]
    [StringLength(8192)]
    public string RecipientToken { get; set; } = string.Empty;

    [StringLength(8192)]
    public string? CaptchaToken { get; set; }
}

public sealed class UnsubscribeFromViewModel
{
    [Required(ErrorMessage = "Please select a reason.")]
    public Guid? ReasonGuid { get; set; }

    public List<SelectListItem> Reasons { get; init; } = new();

    public string RecipientToken { get; init; } = string.Empty;

    public string? Error { get; init; }
    public bool IsPreview { get; init; }

    public bool SkipCaptchaValidation { get; init; }
}

public sealed class UnsubscribePageViewModel
{
    public HeroViewModel Hero { get; init; } = new();

    public string Content { get; init; } = string.Empty;

    public UnsubscribeFromViewModel Form { get; init; } = new();
}

public sealed class UnsubscribeResultViewModel
{
    public bool IsConfirmed { get; init; }

    public string? ReturnUrl { get; init; }
}