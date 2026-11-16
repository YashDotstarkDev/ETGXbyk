using System.Collections.Generic;
using System.Linq;
using ETG;
using ETG.Core.Content;
using ETG.Module.Booking.Classes.Info;
using ETG.Web.Rendering;

namespace ETG.Web.Features.Payment;

public class PaymentOptionItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PaymentPageViewModel
{
    public string Heading { get; set; } = "Client Payments";
    public string PaymentPageContent { get; set; } = string.Empty;
    public string ThankYouCopy { get; set; } = string.Empty;
    public ImageAssetViewModel? HeroImage { get; set; }
    public string? HeroImageUrl { get; set; }
    public List<PaymentOptionItemViewModel> PaymentOptions { get; set; } = new();
    public IEnumerable<FAQ> FAQs { get; set; } = Enumerable.Empty<FAQ>();

    public string TravelpayCSSLink { get; set; } = string.Empty;
    public string TravelpayJSLink { get; set; } = string.Empty;
    public string TravelpayBS5JSLink { get; set; } = string.Empty;

    public PaymentInfo? Payment { get; set; }
    public bool HasWebPageContext { get; set; }

    public static PaymentPageViewModel Create(
        PaymentPage? page,
        IEnumerable<FAQ>? faqs,
        WebsiteGlobalContent globalContent)
    {
        var model = new PaymentPageViewModel
        {
            Heading = !string.IsNullOrWhiteSpace(page?.Heading) ? page.Heading : "Client Payments",
            PaymentPageContent = page?.PaymentPageContent ?? string.Empty,
            ThankYouCopy = page?.PaymentThankyoupageContent ?? string.Empty,
            TravelpayCSSLink = globalContent?.TravelPayCSSLink ?? string.Empty,
            TravelpayJSLink = globalContent?.TravelPayJSLink ?? string.Empty,
            TravelpayBS5JSLink = globalContent?.TravelPayBS5JSLink ?? string.Empty,
            FAQs = faqs ?? Enumerable.Empty<FAQ>()
        };

        if (page?.PaymentThankyouPageHeroImage?.FirstOrDefault() is MediaFile heroMedia)
        {
            model.HeroImage = ImageAssetViewModel.Create(heroMedia);
            model.HeroImageUrl = heroMedia.LegacyMediaFileAsset?.Url;
        }

        if (page?.SelectedPaymentOptions != null)
        {
            model.PaymentOptions = page.SelectedPaymentOptions
                .Where(opt => opt != null)
                .Select(opt => new PaymentOptionItemViewModel
                {
                    Name = opt.PaymentOptionName,
                    Description = opt.PaymentOptionDescription
                })
                .ToList();
        }

        return model;
    }
}
