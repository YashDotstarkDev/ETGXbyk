using ETG.Web.Rendering;

namespace ETG.Web.Components.ViewComponents.Footer;

/// <summary>
/// Top-level footer view model — everything the Razor view needs, pre-resolved.
/// </summary>
public class FooterViewModel
{
    /// <summary>Site logo from WebsiteGlobalContent.</summary>
    public ImageAssetViewModel? Logo { get; init; }

    /// <summary>Site display name from WebsiteGlobalContent.</summary>
    public string SiteName { get; init; } = "";

    /// <summary>Optional tagline / strapline beneath the logo.</summary>
    public string Tagline { get; init; } = "";

    public bool HasTagline => !string.IsNullOrWhiteSpace(Tagline);

    /// <summary>Social media icons row.</summary>
    public IReadOnlyList<FooterSocialLinkViewModel> SocialLinks { get; init; } = [];

    /// <summary>
    /// Footer navigation columns — each Level-0 NavigationItem becomes a column.
    /// Its children (Level-1) are the links listed below the column heading.
    /// </summary>
    public IReadOnlyList<FooterColumnViewModel> Columns { get; init; } = [];

    /// <summary>Bottom bar links (Privacy Policy, Terms, etc.).</summary>
    public IReadOnlyList<FooterBottomLinkViewModel> BottomLinks { get; init; } = [];

    /// <summary>Contact details model for fetching the contact details</summary>

    public FooterContactViewModel? Contact { get; init; }

    /// <summary>website layout model fields</summary>

    public ETG.Web.Components.ViewComponents.Shared.WebsiteLayoutContentViewModel? Layout { get; init; }

    /// <summary>Copyright line, e.g. "© 2025 Company. All rights reserved."</summary>
    public string CopyrightText { get; init; } = "";
}

/// <summary>One column in the footer link grid.</summary>
public class FooterColumnViewModel
{
    /// <summary>Column heading text, e.g. "Company", "Products".</summary>
    public string Heading { get; init; } = "";

    /// <summary>Optional URL — makes the heading itself a link.</summary>
    public string? HeadingUrl { get; init; }

    public IReadOnlyList<FooterLinkViewModel> Links { get; init; } = [];
}

/// <summary>An individual link inside a footer column.</summary>
public class FooterLinkViewModel
{
    public string Label { get; init; } = "";
    public string Url { get; init; } = "#";
    public string Target { get; init; } = "_self";
    public string? Icon { get; init; }
}

/// <summary>A social media icon + link.</summary>
public class FooterSocialLinkViewModel
{
    /// <summary>Accessible label, e.g. "LinkedIn".</summary>
    public string Title { get; init; } = "";

    /// <summary>Bootstrap Icons class, e.g. "bi bi-linkedin".</summary>
    public string Icon { get; init; } = "";

    public string Url { get; init; } = "#";
    public string Target { get; init; } = "_blank";
}

/// <summary>A link in the bottom bar (Privacy Policy, Terms, etc.).</summary>
public class FooterBottomLinkViewModel
{
    public string Label { get; init; } = "";
    public string Url { get; init; } = "#";
    public string Target { get; init; } = "_self";
}

public class FooterContactViewModel
{
    public string? OpeningHours { get; init; }
    public string? Address1 { get; init; }
    public string? Address2 { get; init; }
    public string? PhoneNumber { get; init; }
    public string? PhoneNumber2 { get; init; }
    public string? EmailAddress { get; init; }
    public string? FacebookUrl { get; init; }
    public string? InstagramUrl { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? YouTubeUrl { get; init; }
}



