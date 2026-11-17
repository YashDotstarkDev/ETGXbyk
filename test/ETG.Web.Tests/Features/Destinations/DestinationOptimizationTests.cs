using System;
using System.Collections.Generic;
using CMS.Websites.Routing;
using ETG.Core.Content;
using ETG.Web.Features.Destinations.Operations;
using Kentico.Content.Web.Mvc;
using NUnit.Framework;

namespace ETG.Web.Tests.Features.Destinations;

[TestFixture]
public class DestinationOptimizationTests : BaseWebTest
{
    [Test]
    public void GetParentDestinationGuidQuery_CacheKey_IsParentPageId()
    {
        var query = new GetParentDestinationGuidQuery("ETG", 456);

        Assert.That(query.CacheValueKey, Is.EqualTo("456"));
        Assert.That(query.ParentPageId, Is.EqualTo(456));
        Assert.That(query.ChannelName, Is.EqualTo("ETG"));
    }

    [Test]
    public void GetDestinationArticlesQuery_CacheValueKey_IsDeterministicAndSorted()
    {
        var guidA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guidB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var set1 = new HashSet<Guid> { guidB, guidA };
        var set2 = new HashSet<Guid> { guidA, guidB };

        var query1 = new GetDestinationArticlesQuery("ETG", set1);
        var query2 = new GetDestinationArticlesQuery("ETG", set2);

        Assert.That(query1.CacheValueKey, Is.EqualTo(query2.CacheValueKey));
        Assert.That(query1.CacheValueKey, Is.EqualTo($"{guidA}|{guidB}"));
    }

    [Test]
    public void MainDestinationsQuery_CacheValueKey_MatchesWebPageKey()
    {
        var page = new RoutedWebPage
        {
            WebPageItemID = 50,
            WebsiteChannelName = "ETG",
            LanguageName = "en"
        };

        var query = new MainDestinationsQuery(page, "ETG", "/destinations");

        Assert.That(query.CacheValueKey, Is.EqualTo("50|ETG|en"));
        Assert.That(query.ParentPath, Is.EqualTo("/destinations"));
    }
}
