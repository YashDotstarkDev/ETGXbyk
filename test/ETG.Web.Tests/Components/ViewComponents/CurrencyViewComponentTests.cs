using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ETG.Web.Components.ViewComponents.CurrencyDropdown;
using ETG.Web.Controllers;
using ETG.Web.Services.Currency;
using ETG.Web.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents;

[TestFixture]
public class CurrencyViewComponentTests : BaseWebTest
{
    private Mock<ICurrencyService> _currency = null!;
    private Mock<IExchangeRateProvider> _rates = null!;
    private DefaultHttpContext _context = null!;
    private HttpContextAccessor _accessor = null!;
    private CurrencyService _service = null!;
    private CurrencyController _controller = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        _currency = new Mock<ICurrencyService>();
        _rates = new Mock<IExchangeRateProvider>();
        _context = new DefaultHttpContext();
        _accessor = new HttpContextAccessor { HttpContext = _context };

        _service = new CurrencyService(_accessor, _rates.Object);

        _controller = new CurrencyController(_currency.Object)
        {
            Url = new UrlHelper(new ActionContext(
                _context,
                new RouteData(),
                new ActionDescriptor()))
        };
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            _controller?.Dispose();
        }
        finally
        {
            base.TearDown();
        }
    }

    [TestCaseSource(typeof(CurrencyCodeCases))]
    public void CurrencyCode_IsValidatedBeforeBeingSet(
        string? code, bool valid)
    {
        Assert.That(
            _service.IsValidCurrencyCode(code),
            Is.EqualTo(valid));

        _currency.Setup(x => x.IsValidCurrencyCode(code))
            .Returns(() => _service.IsValidCurrencyCode(code));

        var result = _controller.Set(code!, "/booking");

        if (valid)
        {
            Assert.That(result, Is.TypeOf<LocalRedirectResult>());
            Assert.That(
                ((LocalRedirectResult)result).Url,
                Is.EqualTo("/booking"));

            _currency.Verify(
                x => x.SetCurrentCurrency(code!),
                Times.Once);
        }
        else
        {
            Assert.That(result, Is.TypeOf<BadRequestResult>());

            _currency.Verify(
                x => x.SetCurrentCurrency(It.IsAny<string>()),
                Times.Never);
        }
    }

    [TestCaseSource(typeof(CurrencyRedirectCases))]
    public void Controller_AllowsOnlyLocalRedirects(
        string? returnUrl, string expected)
    {
        _currency.Setup(x => x.IsValidCurrencyCode("AUD"))
            .Returns(true);

        var result = _controller.Set("AUD", returnUrl);

        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(
            ((LocalRedirectResult)result).Url,
            Is.EqualTo(expected));

        _currency.Verify(
            x => x.SetCurrentCurrency("AUD"),
            Times.Once);
    }

    [TestCaseSource(typeof(CurrencyCookieReadCases))]
    public void Service_UsesCookieOrHostFallback(
        string? host, string? cookie, string expected)
    {
        if (host == null)
        {
            _accessor.HttpContext = null;
        }
        else
        {
            _context.Request.Host = new HostString(host);

            if (cookie != null)
            {
                _context.Request.Headers["Cookie"] =
                    $"CurrentCurrency={cookie}";
            }
        }

        Assert.That(
            _service.GetCurrentCurrency(),
            Is.EqualTo(expected));
    }

    [TestCaseSource(typeof(CurrencyCookieWriteCases))]
    public void Service_WritesCookieWithCurrentSettings(
        string code, bool hasContext)
    {
        if (!hasContext)
        {
            _accessor.HttpContext = null;

            Assert.DoesNotThrow(
                () => _service.SetCurrentCurrency(code));

            Assert.That(
                _context.Response.Headers.ContainsKey("Set-Cookie"),
                Is.False);

            return;
        }

        var cookies = new Mock<IResponseCookies>();
        var response = new Mock<HttpResponse>();
        var context = new Mock<HttpContext>();

        response.SetupGet(x => x.Cookies).Returns(cookies.Object);
        context.SetupGet(x => x.Response).Returns(response.Object);

        _accessor.HttpContext = context.Object;

        CookieOptions? options = null;
        var expectedCode = code.ToUpperInvariant();

        cookies.Setup(x => x.Append(
                "CurrentCurrency",
                expectedCode,
                It.IsAny<CookieOptions>()))
            .Callback<string, string, CookieOptions>(
                (_, _, value) => options = value);

        var before = DateTimeOffset.UtcNow.AddYears(1);

        _service.SetCurrentCurrency(code);

        var after = DateTimeOffset.UtcNow.AddYears(1);

        cookies.Verify(
            x => x.Append(
                "CurrentCurrency",
                expectedCode,
                It.IsAny<CookieOptions>()),
            Times.Once);

        Assert.That(options, Is.Not.Null);
        Assert.That(options!.Expires.HasValue, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(options.Path, Is.EqualTo("/"));
            Assert.That(options.Secure, Is.True);
            Assert.That(options.SameSite, Is.EqualTo(SameSiteMode.Lax));
            Assert.That(options.IsEssential, Is.True);
            Assert.That(options.HttpOnly, Is.False);
            Assert.That(options.Expires!.Value, Is.InRange(before, after));
        });
    }

    [TestCaseSource(typeof(CurrencyViewCases))]
    public void Component_ReturnsPricingOrFallback(string scenario)
    {
        var supported = new[] { "NZD", "AUD" };

        if (scenario == "Throws")
        {
            _currency.Setup(x => x.GetSupportedCurrencies())
                .Throws(new InvalidOperationException("Service failed"));
        }
        else
        {
            _currency.Setup(x => x.GetSupportedCurrencies())
                .Returns(() => scenario switch
                {
                    "Null" => null!,
                    "Empty" => Array.Empty<string>(),
                    _ => supported
                });
        }

        var pricing = new CurrentCurrencyPricing("NZD", 1.1m);

        var component = new CurrencyDropdownViewComponent(
            _currency.Object,
            pricing);

        var result = component.Invoke();

        Assert.That(result, Is.TypeOf<ViewViewComponentResult>());

        var view = (ViewViewComponentResult)result;

        Assert.That(
            view.ViewName,
            Is.EqualTo(
                "~/Components/ViewComponents/CurrencyDropdown/CurrencyDropdown.cshtml"));

        Assert.That(
            view.ViewData.Model,
            Is.TypeOf<CurrencyDropdownViewModel>());

        var model = (CurrencyDropdownViewModel)view.ViewData.Model!;
        var fallback = scenario == "Throws" || scenario == "Null";

        Assert.Multiple(() =>
        {
            Assert.That(
                model.Currencies,
                Is.EqualTo(
                    fallback || scenario == "Empty"
                        ? Array.Empty<string>()
                        : supported.OrderBy(x => x).ToArray()));

            Assert.That(
                model.CurrentCurrency,
                Is.EqualTo(fallback ? "" : "NZD"));

            Assert.That(
                model.CurrentCurrencySymbol,
                Is.EqualTo(
                    fallback ? "" : pricing.CurrentCurrencySymbol));

            Assert.That(
                model.ConversionRate,
                Is.EqualTo(fallback ? 0m : 1.1m));

            Assert.That(
                model.CurrencyApplyDiscounts,
                Is.EqualTo(!fallback));
        });
    }

    [TestCaseSource(typeof(CurrencyRateCases))]
    public void Service_ReturnsCurrentExchangeRateBehavior(
        string code, double providerRate, double expected)
    {
        _rates.Setup(x => x.GetRate(code.ToUpperInvariant()))
            .Returns((decimal)providerRate);

        Assert.That(
            _service.GetConversionRate(code),
            Is.EqualTo((decimal)expected));

        _rates.Verify(
            x => x.GetRate(It.IsAny<string>()),
            code.Equals("AUD", StringComparison.OrdinalIgnoreCase)
                ? Times.Never()
                : Times.Once());
    }

    [TestCaseSource(typeof(CurrencyFailureCases))]
    public void Exceptions_FollowCurrentBehavior(string scenario)
    {
        switch (scenario)
        {
            case "Validation":
                _currency.Setup(x => x.IsValidCurrencyCode("AUD"))
                    .Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(
                    () => _controller.Set("AUD", "/"));
                break;

            case "SetCurrency":
                _currency.Setup(x => x.IsValidCurrencyCode("AUD"))
                    .Returns(true);

                _currency.Setup(x => x.SetCurrentCurrency("AUD"))
                    .Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(
                    () => _controller.Set("AUD", "/"));
                break;

            case "ExchangeRate":
                _rates.Setup(x => x.GetRate("NZD"))
                    .Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(
                    () => _service.GetConversionRate("NZD"));
                break;

            case "NullRateCode":
                Assert.Throws<NullReferenceException>(
                    () => _service.GetConversionRate(null!));
                break;

            case "NullCookieCode":
                Assert.Throws<NullReferenceException>(
                    () => _service.SetCurrentCurrency(null!));
                break;

            default:
                Assert.Fail($"Unknown scenario: {scenario}");
                break;
        }
    }

    [TestCaseSource(typeof(CurrencyPricingCodeCases))]
    public void Pricing_UsesCurrentDefaultAndDiscountRules(
        string? code, string expected, bool discounts)
    {
        var pricing = new CurrentCurrencyPricing(code, 1m);

        Assert.Multiple(() =>
        {
            Assert.That(pricing.CurrentCurrency, Is.EqualTo(expected));

            Assert.That(
                pricing.CurrentCurrencyIsAUD,
                Is.EqualTo(expected == "AUD"));

            Assert.That(
                pricing.CurrencyAppliesDiscounts,
                Is.EqualTo(discounts));
        });
    }

    [TestCaseSource(typeof(CurrencyConversionCases))]
    public void Pricing_ConvertsAndFormatsUsingCurrentRules(
        string code,
        double rate,
        int amount,
        int forward,
        int reverse)
    {
        var pricing = new CurrentCurrencyPricing(code, (decimal)rate);
        var expectedText = $"{code} {forward}";

        Assert.Multiple(() =>
        {
            Assert.That(pricing.ConversionRate, Is.EqualTo((decimal)rate));

            Assert.That(
                pricing.ConvertAUDToCurrentCurrency(amount),
                Is.EqualTo(forward));

            Assert.That(
                pricing.ConvertAUDToCurrentCurrency((decimal)amount),
                Is.EqualTo(forward));

            Assert.That(
                pricing.ConvertAUDToCurrentCurrency((double)amount),
                Is.EqualTo(forward));

            Assert.That(
                pricing.ConvertCurrentCurrencyToAUD((decimal)amount),
                Is.EqualTo(reverse));

            Assert.That(
                pricing.ConvertCurrentCurrencyToAUD((double)amount),
                Is.EqualTo(reverse));

            Assert.That(
                pricing.GetCurrentCurrencyDisplayPrice(amount),
                Is.EqualTo(expectedText));

            Assert.That(
                pricing.GetCurrentCurrencyDisplayPrice((decimal)amount),
                Is.EqualTo(expectedText));

            Assert.That(
                pricing.GetCurrentCurrencyDisplayPrice((double)amount),
                Is.EqualTo(expectedText));
        });
    }



    // Non-static case providers.
    // NUnit constructs these classes and enumerates their cases.
    public abstract class CurrencyCaseSource : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class CurrencyCodeCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var code in CurrencyConstants.SupportedCurrencies)
            {
                yield return new TestCaseData(code, true);
                yield return new TestCaseData(code.ToLowerInvariant(), true);
            }

            foreach (var code in new string?[]
            {
            null, "", " ", " AUD ", "__UNKNOWN__",
            "../AUD", "<script>alert(1)</script>"
            })
            {
                yield return new TestCaseData(code, false);
            }
        }
    }

    public sealed class CurrencyRedirectCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData("/booking", "/booking");

            foreach (var url in new string?[]
            {
            null, "", " ", "https://evil.example",
            "//evil.example", "/\\evil.example"
            })
            {
                yield return new TestCaseData(url, "/");
            }
        }
    }

    public sealed class CurrencyCookieReadCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            // Null host represents missing HttpContext.
            yield return new TestCaseData(null, null, "AUD");
            yield return new TestCaseData("example.nz", "aud", "AUD");
            yield return new TestCaseData("example.nz", null, "NZD");
            yield return new TestCaseData("example.com", null, "AUD");
            yield return new TestCaseData("example.com", "", "AUD");
            yield return new TestCaseData(
                "example.com", "__TAMPERED__", "AUD");
        }
    }

    public sealed class CurrencyCookieWriteCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData("aud", true);
            yield return new TestCaseData("nzd", true);

            // Current service writes unsupported codes without validation.
            yield return new TestCaseData("__TAMPERED__", true);
            yield return new TestCaseData("AUD", false);
        }
    }

    public sealed class CurrencyViewCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var scenario in new[]
            {
            "Success", "Throws", "Null", "Empty"
        })
            {
                yield return new TestCaseData(scenario);
            }
        }
    }

    public sealed class CurrencyRateCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData("AUD", 99d, 1d);

            foreach (var rate in new[] { 1.1d, 0d, -1d })
            {
                yield return new TestCaseData("nzd", rate, rate);
            }
        }
    }

    public sealed class CurrencyFailureCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var scenario in new[]
            {
            "Validation", "SetCurrency", "ExchangeRate",
            "NullRateCode", "NullCookieCode"
        })
            {
                yield return new TestCaseData(scenario);
            }
        }
    }

    public sealed class CurrencyPricingCodeCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData(null, "AUD", true);
            yield return new TestCaseData("", "AUD", true);
            yield return new TestCaseData("AUD", "AUD", true);
            yield return new TestCaseData("NZD", "NZD", true);
            yield return new TestCaseData("GBP", "GBP", false);
            yield return new TestCaseData("aud", "aud", false);
            yield return new TestCaseData(" ", " ", false);
        }
    }

    public sealed class CurrencyConversionCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData("AUD", 1d, 20, 20, 20);
            yield return new TestCaseData("NZD", 1.1d, 11, 13, 10);
            yield return new TestCaseData("GBP", 0.5d, 20, 10, 40);

            // Zero rate: forward is zero; reverse falls back to input.
            // Use a nonzero input so display formatting remains unambiguous.
            yield return new TestCaseData("NZD", -1d, 10, -10, -10);
        }
    }

    public sealed class CurrencyValidationGapCases : CurrencyCaseSource
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var scenario in new[]
            {
            "UnsupportedCookie", "ZeroRate", "NegativeRate"
        })
            {
                yield return new TestCaseData(scenario);
            }
        }
    }
}