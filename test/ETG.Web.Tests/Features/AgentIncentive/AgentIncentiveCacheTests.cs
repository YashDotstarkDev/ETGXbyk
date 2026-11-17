using System;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Features.AgentIncentive.Operations;
using Kentico.Content.Web.Mvc;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.AgentIncentive;

[TestFixture]
public class AgentIncentiveCacheTests : BaseWebTest
{
    private static RoutedWebPage CreateRoutedPage(int id, string channel, string language)
    {
        return new RoutedWebPage
        {
            WebPageItemID = id,
            WebsiteChannelName = channel,
            LanguageName = language,
            ContentTypeName = Incentive.CONTENT_TYPE_NAME
        };
    }

    [Test]
    public void CacheValueKey_DistinguishesChannelAndLanguage()
    {
        var page1 = CreateRoutedPage(42, "ETG_AU", "en-AU");
        var page2 = CreateRoutedPage(42, "ETG_UK", "en-GB");

        var query1 = new AgentIncentivePageQuery(page1, false);
        var query2 = new AgentIncentivePageQuery(page2, false);

        Assert.That(query1.CacheValueKey, Is.Not.EqualTo(query2.CacheValueKey));
        Assert.That(query1.CacheValueKey, Does.Contain("ETG_AU").And.Contain("en-AU"));
        Assert.That(query2.CacheValueKey, Does.Contain("ETG_UK").And.Contain("en-GB"));
    }

    [Test]
    public void CacheValueKey_DistinguishesPreviewMode()
    {
        var page = CreateRoutedPage(10, "ETG", "en");

        var liveQuery = new AgentIncentivePageQuery(page, false);
        var previewQuery = new AgentIncentivePageQuery(page, true);

        Assert.That(liveQuery.CacheValueKey, Is.Not.EqualTo(previewQuery.CacheValueKey));
        Assert.That(liveQuery.CacheValueKey, Does.EndWith(":False"));
        Assert.That(previewQuery.CacheValueKey, Does.EndWith(":True"));
    }

    [Test]
    public void IChannelContentQuery_ReturnsWebsiteChannelName()
    {
        var page = CreateRoutedPage(10, "Channel123", "en");
        var query = new AgentIncentivePageQuery(page, false);

        IChannelContentQuery channelQuery = query;
        Assert.That(channelQuery.ChannelName, Is.EqualTo("Channel123"));
    }
}
