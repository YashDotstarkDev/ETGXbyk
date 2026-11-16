using System;
using System.ComponentModel.DataAnnotations;

namespace ETG.Web.Configuration;

public sealed class UnsubscribeOptions
{
    public const string SectionName = "Unsubscribe";

    [Range(1, 168)]
    public int RecipientLinkHours { get; set; } = 24;

    [Required]
    [MinLength(1)]
    public string[] CaptchaAllowedHostnames { get; set; }
        = Array.Empty<string>();
}