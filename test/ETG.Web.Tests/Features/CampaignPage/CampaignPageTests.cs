using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using ETG.Web.Features.CampaignPage;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Moq;
using NUnit.Framework;

using ContainerContent = ETG.Core.Content.Container;
using MediaFile = ETG.Core.Content.MediaFile;

namespace ETG.Web.Tests.Features.CampaignPage;

[TestFixture]
[Category("CurrentBehavior")]
public class CampaignPageViewModelTests : BaseWebTest
{
    private Mock<IWebPageUrlRetriever> _urlRetriever = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _urlRetriever = new Mock<IWebPageUrlRetriever>();
    }

    [TestCaseSource(typeof(CampaignMappingCases))]
    public async Task CreateAsync_MapsNamesAndFallbacks(
        string? name,
        string? heading,
        string expectedHeading,
        string expectedBanner)
    {
        var page = new ContainerContent
        {
            ContainerName = name!,
            ContainerHeading = heading!
        };

        var model = await CampaignPageViewModel.CreateAsync(
            page, _urlRetriever.Object);

        Assert.Multiple(() =>
        {
            Assert.That(model.PageName, Is.EqualTo(name ?? ""));
            Assert.That(model.PageHero.Heading,
                Is.EqualTo(expectedHeading));
            Assert.That(model.BannerLabel,
                Is.EqualTo(expectedBanner));
        });

        _urlRetriever.VerifyNoOtherCalls();
    }

    [TestCaseSource(typeof(CampaignTextCases))]
    public async Task CreateAsync_MapsTextFields(string? value)
    {
        var page = new ContainerContent
        {
            ContainerHeadingText = value!,
            ContainerHeroImageAltTag = value!,
            ContainerHeroAccreditation = value!,
            ContainerHeroPhone = value!
        };

        var model = await CampaignPageViewModel.CreateAsync(
            page, _urlRetriever.Object);

        Assert.Multiple(() =>
        {
            Assert.That(model.PageHero.HeadingSummary,
                Is.EqualTo(value ?? ""));
            Assert.That(model.PageHero.HeroImageAltText,
                Is.EqualTo(value ?? ""));
            Assert.That(model.PageHero.HeroCaption,
                Is.EqualTo(value ?? ""));
            Assert.That(model.PageHero.HeroPhone,
                Is.EqualTo(value ?? ""));
            Assert.That(model.RelatedArticles, Is.Empty);
            Assert.That(model.PageHero.DisableOverlay, Is.False);
        });
    }

    [TestCaseSource(typeof(CampaignLinkedContentCases))]
    public async Task CreateAsync_IgnoresMissingOrWrongLinkedContent(
        string imageKind,
        string contactKind)
    {
        var page = new ContainerContent
        {
            ContainerHeroImage = CreateItems(imageKind)!,
            ContainerHeroContactUsCtaPath = CreateItems(contactKind)!
        };

        var model = await CampaignPageViewModel.CreateAsync(
            page, _urlRetriever.Object);

        Assert.Multiple(() =>
        {
            Assert.That(model.PageHero.HeroImage, Is.Null);
            Assert.That(model.PageHero.HeroContactUsCtaPath, Is.Empty);
        });

        _urlRetriever.VerifyNoOtherCalls();
    }

    [TestCaseSource(typeof(CampaignReferenceCases))]
    public async Task CreateAsync_UsesCurrentNullReferenceBehavior(
        string scenario,
        bool nullRetriever,
        bool expectsException)
    {
        ContainerContent? page = scenario == "NullPage"
            ? null
            : new ContainerContent();

        if (scenario == "Contact")
        {
            page!.ContainerHeroContactUsCtaPath =
                new IContentItemFieldsSource[]
                {
                    new ContainerContent()
                };
        }

        if (scenario == "InvalidMedia")
        {
            page!.ContainerHeroImage =
                new IContentItemFieldsSource[]
                {
                    new MediaFile { SystemFields = null! }
                };
        }

        var retriever = nullRetriever ? null : _urlRetriever.Object;

        if (expectsException)
        {
            Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await CampaignPageViewModel.CreateAsync(
                    page!, retriever!);
            });
        }
        else
        {
            var model = await CampaignPageViewModel.CreateAsync(
                page!, retriever!);

            Assert.That(model.PageHero.HeroContactUsCtaPath, Is.Empty);
        }

        _urlRetriever.VerifyNoOtherCalls();
    }

    [Test]
    public void Template_RegistersExpectedIdentifier()
    {
        var registrations = typeof(CampaignPageTemplate)
            .Assembly
            .GetCustomAttributesData()
            .Where(attribute =>
                attribute.AttributeType ==
                typeof(RegisterPageTemplateAttribute))
            .Where(attribute =>
            {
                var parameters =
                    attribute.Constructor.GetParameters();

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name == "identifier")
                    {
                        return Equals(
                            attribute.ConstructorArguments[i].Value,
                            "ETG.CampaignTemplate");
                    }
                }

                return false;
            })
            .ToArray();

        Assert.That(registrations, Has.Length.EqualTo(1));
    }

    private System.Collections.Generic
        .IEnumerable<IContentItemFieldsSource>? CreateItems(string kind)
    {
        return kind switch
        {
            "Null" => null,
            "Empty" => Array.Empty<IContentItemFieldsSource>(),
            "NullElement" => new IContentItemFieldsSource[] { null! },
            "WrongType" => new IContentItemFieldsSource[]
            {
                new Mock<IContentItemFieldsSource>().Object
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}

public class CampaignMappingCases : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var token = Guid.NewGuid().ToString("N");

        var names = new (string? Value, string Fallback, string Banner)[]
        {
            (null, "", "Campaign hero banner"),
            ("", "", "Campaign hero banner"),
            (" \t", " \t", "Campaign hero banner"),
            ($"Campaign-{token}",
             $"Campaign-{token}",
             $"Campaign-{token} hero banner")
        };

        foreach (var name in names)
        {
            var headings = new (string? Value, string Expected)[]
            {
                (null, name.Fallback),
                ("", name.Fallback),
                (" \t", name.Fallback),
                ($"Heading-{token}", $"Heading-{token}")
            };

            foreach (var heading in headings)
            {
                yield return new TestCaseData(
                    name.Value,
                    heading.Value,
                    heading.Expected,
                    name.Banner);
            }
        }
    }
}

public class CampaignTextCases : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var token = Guid.NewGuid().ToString("N");

        foreach (var value in new string?[]
        {
            null,
            "",
            " \t\r\n",
            $"Details-{token}",
            $"日本語-é-مرحبا-{token}",
            $"<script>alert('{token}')</script>"
        })
        {
            yield return new TestCaseData(value);
        }
    }
}

public class CampaignLinkedContentCases : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var kinds = new[]
        {
            "Null", "Empty", "WrongType", "NullElement"
        };

        foreach (var imageKind in kinds)
            foreach (var contactKind in kinds)
            {
                yield return new TestCaseData(imageKind, contactKind);
            }
    }
}

public class CampaignReferenceCases : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new TestCaseData("NullPage", false, true);
        yield return new TestCaseData("NullPage", true, true);
        yield return new TestCaseData("NoContact", true, false);
        yield return new TestCaseData("Contact", true, true);
        yield return new TestCaseData("InvalidMedia", false, true);
    }
}

[TestFixture]
[Category("CurrentBehavior")]
public class CampaignPageWhitespaceTests : BaseWebTest
{
    [TestCaseSource(typeof(CampaignWhitespaceCases))]
    public async Task CreateAsync_BlankHeading_PreservesNameFallback(
        string name,
        string? heading)
    {
        var page = new ContainerContent
        {
            ContainerName = name,
            ContainerHeading = heading!
        };

        var model = await CampaignPageViewModel.CreateAsync(
            page, new Mock<IWebPageUrlRetriever>().Object);

        Assert.Multiple(() =>
        {
            Assert.That(model.PageHero.Heading, Is.EqualTo(name));
            Assert.That(model.BannerLabel,
                Is.EqualTo("Campaign hero banner"));
        });
    }
}

public class CampaignWhitespaceCases : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        foreach (var name in new[] { " ", "\t", "\r\n" })
            foreach (var heading in new string?[] { null, "", " \t" })
            {
                yield return new TestCaseData(name, heading);
            }
    }
}