using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.GenericPage;
using ETG.Web.Infrastructure;
using Kentico.Content.Web.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.GenericPage;

[TestFixture]
public class GenericPageOptimizationTests : BaseWebTest
{
    private static RoutedWebPage CreateRoutedPage(int id, string channel, string language)
    {
        return new RoutedWebPage
        {
            WebPageItemID = id,
            WebsiteChannelName = channel,
            LanguageName = language,
            ContentTypeName = Container.CONTENT_TYPE_NAME
        };
    }

    [Test]
    public void GenericPageQuery_CacheKey_IncludesItemChannelAndLanguage()
    {
        var page = CreateRoutedPage(123, "ETG_NZ", "en-NZ");
        var query = new GenericPageQuery(page);

        Assert.That(query.CacheValueKey, Is.EqualTo("123|ETG_NZ|en-NZ"));
    }

    [Test]
    public async Task ExpiryService_ActivePage_ReturnsNullWithoutRedirect()
    {
        var urlRetriever = new Mock<IWebPageUrlRetriever>(MockBehavior.Strict);
        var executor = new Mock<IContentQueryExecutor>(MockBehavior.Strict);
        var channel = new Mock<IWebsiteChannelContext>(MockBehavior.Strict);

        var service = new ContainerExpiryService(
            urlRetriever.Object,
            executor.Object,
            channel.Object,
            NullLogger<ContainerExpiryService>.Instance);

        var container = new Container
        {
            ContainerExpiry = DateTime.UtcNow.AddDays(7),
            ContainerRedirect = new List<IContentItemFieldsSource>()
        };

        var redirect = await service.GetExpiryRedirectUrlAsync(container);

        Assert.That(redirect, Is.Null);
    }

    [Test]
    public async Task ExpiryService_ZeroOrMinDate_ReturnsNull()
    {
        var service = new ContainerExpiryService(
            Mock.Of<IWebPageUrlRetriever>(),
            Mock.Of<IContentQueryExecutor>(),
            Mock.Of<IWebsiteChannelContext>(),
            NullLogger<ContainerExpiryService>.Instance);

        var containerZero = new Container { ContainerExpiry = DateTimeHelper.ZERO_TIME };
        var containerMin = new Container { ContainerExpiry = DateTime.MinValue };

        Assert.That(await service.GetExpiryRedirectUrlAsync(containerZero), Is.Null);
        Assert.That(await service.GetExpiryRedirectUrlAsync(containerMin), Is.Null);
    }

    [Test]
    public async Task ExpiryService_ExpiredPage_ResolvesPreloadedRedirectUrl()
    {
        var urlRetriever = new Mock<IWebPageUrlRetriever>(MockBehavior.Strict);
        var executor = new Mock<IContentQueryExecutor>(MockBehavior.Strict);
        var channel = new Mock<IWebsiteChannelContext>(MockBehavior.Strict);

        var targetFields = new WebPageFields
        {
            WebPageItemID = 999,
            WebPageItemWebsiteChannelId = 1
        };

        var targetPage = new Mock<IWebPageFieldsSource>(MockBehavior.Strict);
        targetPage.Setup(p => p.SystemFields).Returns(targetFields);

        var expectedUrl = new WebPageUrl("/special-offers", "https://example.com/special-offers");

        urlRetriever
            .Setup(u => u.Retrieve(targetPage.Object, null))
            .ReturnsAsync(expectedUrl);

        var service = new ContainerExpiryService(
            urlRetriever.Object,
            executor.Object,
            channel.Object,
            NullLogger<ContainerExpiryService>.Instance);

        var container = new Container
        {
            ContainerExpiry = DateTime.UtcNow.AddMinutes(-5),
            ContainerRedirect = new[] { targetPage.Object },
            SystemFields = new WebPageFields { WebPageItemID = 100 }
        };

        var redirect = await service.GetExpiryRedirectUrlAsync(container);

        Assert.That(redirect, Is.EqualTo("/special-offers"));
    }

    [Test]
    public void DefaultQueryCacheSettings_DefaultsToEnabledWithFiveMinutes()
    {
        var settings = new ETG.Core.Operations.DefaultQueryCacheSettings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.IsEnabled, Is.True);
            Assert.That(settings.IsSlidingExpiration, Is.True);
            Assert.That(settings.CacheItemDuration, Is.EqualTo(TimeSpan.FromMinutes(5)));
        });
    }
}
