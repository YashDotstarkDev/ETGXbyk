using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ETG.Web.Components.ViewComponents.TrackingConsent;
using ETG.Web.Tests;
using NUnit.Framework;

namespace ETG.Web.Tests.Components.ViewComponents
{
    [TestFixture]
    public class TrackingConsentViewComponentTests : BaseWebTest
    {
        [TestCaseSource(typeof(ConsentCleanupCases))]
        public void CleanConsentText_ReturnsExpectedText(
            string? input,
            string expected)
        {
            Assert.That(Clean(input), Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(ConsentUnsafeMarkupCases))]
        [Category("SecurityGap")]
        public void CleanConsentText_DoesNotSanitizeUnsafeMarkup(
            string input,
            string expected)
        {
            // Passing characterization test:
            // cleanup is not an HTML sanitizer.
            Assert.That(Clean(input), Is.EqualTo(expected));
        }

        [Test]
        public void InvokeAsync_NullConsentProvider_PropagatesArgumentNullException()
        {
            var component = new TrackingConsentViewComponent(
                consentInfoProvider: null!,
                consentAgreementService: null!,
                currentLanguageRetriever: null!,
                webPageDataContextRetriever: null!,
                urlRetriever: null!,
                websiteChannelContext: null!,
                currentCookieLevelProvider: null!,
                cookieAccessor: null!,
                mediator: null!);

            var exception = Assert.ThrowsAsync<ArgumentNullException>(
                async () => { await component.InvokeAsync(); });

            Assert.That(exception!.ParamName, Is.EqualTo("infoProvider"));
        }

        [Test]
        public void ConsentViewModel_Defaults_AreUninitialized()
        {
            var model = new ConsentViewModel();

            Assert.Multiple(() =>
            {
                Assert.That(model.ConsentShortText, Is.Null);
                Assert.That(model.ConsentFullText, Is.Null);
                Assert.That(model.PrivacyPageUrl, Is.Null);
                Assert.That(model.ReturnPageUrl, Is.Null);
                Assert.That(model.Layout, Is.Null);
                Assert.That(model.IsAgreed, Is.False);
                Assert.That(model.IsRevoke, Is.False);
            });
        }

        [TestCaseSource(typeof(ConsentModelCases))]
        public void ConsentViewModel_PreservesAssignedValues(
            string? value,
            bool agreed,
            bool revoke)
        {
            var model = new ConsentViewModel
            {
                ConsentShortText = value!,
                ConsentFullText = value!,
                PrivacyPageUrl = value!,
                ReturnPageUrl = value!,
                IsAgreed = agreed,
                IsRevoke = revoke,
                Layout = null
            };

            Assert.Multiple(() =>
            {
                Assert.That(model.ConsentShortText, Is.EqualTo(value));
                Assert.That(model.ConsentFullText, Is.EqualTo(value));
                Assert.That(model.PrivacyPageUrl, Is.EqualTo(value));
                Assert.That(model.ReturnPageUrl, Is.EqualTo(value));
                Assert.That(model.IsAgreed, Is.EqualTo(agreed));
                Assert.That(model.IsRevoke, Is.EqualTo(revoke));
                Assert.That(model.Layout, Is.Null);
            });
        }

        private static string Clean(string? input)
        {
            // Reflection tests the existing private helper without
            // changing its production visibility.
            var method = typeof(TrackingConsentViewComponent).GetMethod(
                "CleanConsentText",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            return (string)method!.Invoke(
                null,
                new object?[] { input })!;
        }
    }

    public abstract class ConsentTestCases : IEnumerable<TestCaseData>
    {
        public abstract IEnumerator<TestCaseData> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ConsentCleanupCases : ConsentTestCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new TestCaseData(
                new object?[] { null, "" });

            foreach (var input in new[] { "", " ", "\t\r\n", "\u00A0" })
            {
                yield return new TestCaseData(input, "");
            }

            foreach (var text in new[]
            {
                "Tracking consent",
                "日本語-é-مرحبا"
            })
            {
                foreach (var wrapper in new[]
                {
                    "{0}",
                    "<p>{0}</p>",
                    "<P class=\"consent\">{0}</P>",
                    "<pre>{0}</pre>",
                    "<pre><p>{0}</p></pre>"
                })
                {
                    yield return new TestCaseData(
                        string.Format(wrapper, text),
                        text);
                }
            }

            foreach (var br in new[] { "<br>", "<br/>", "<BR />" })
            {
                // Breaks are removed, not replaced with a space.
                yield return new TestCaseData(
                    $"First{br}Second",
                    "FirstSecond");
            }

            yield return new TestCaseData(
                "  First\t \r\nSecond\u00A0Third  ",
                "First Second Third");

            yield return new TestCaseData(
                "<p>Terms &amp; privacy&nbsp;policy</p>",
                "Terms & privacy policy");

            yield return new TestCaseData(
                "&lt;p&gt;Encoded&lt;/p&gt;",
                "<p>Encoded</p>");

            // Only pre-then-p unwrapping is performed.
            yield return new TestCaseData(
                "<p><pre>Text</pre></p>",
                "<pre>Text</pre>");

            yield return new TestCaseData(
                "<p>Unclosed",
                "<p>Unclosed");
        }
    }

    public sealed class ConsentUnsafeMarkupCases : ConsentTestCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var markup in new[]
            {
                "<script>alert(1)</script>",
                "<img src=x onerror=alert(1)>",
                "<a href=\"javascript:alert(1)\">Link</a>"
            })
            {
                yield return new TestCaseData(markup, markup);

                yield return new TestCaseData(
                    $"<p>{markup}</p>",
                    markup);
            }

            yield return new TestCaseData(
                "&lt;script&gt;alert(1)&lt;/script&gt;",
                "<script>alert(1)</script>");
        }
    }

    public sealed class ConsentModelCases : ConsentTestCases
    {
        public override IEnumerator<TestCaseData> GetEnumerator()
        {
            foreach (var value in new string?[]
            {
                null,
                "",
                " ",
                "/privacy-policy",
                "unexpected-value"
            })
            {
                foreach (var agreed in new[] { false, true })
                {
                    foreach (var revoke in new[] { false, true })
                    {
                        yield return new TestCaseData(
                            value,
                            agreed,
                            revoke);
                    }
                }
            }
        }
    }
}