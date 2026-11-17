using ETG.Web.Features.Home.Operations;

namespace ETG.Web.Tests.Features.Home;

/// <summary>
/// Unit tests for <see cref="GetSelectedExperiencesQuery"/> and its cache-key
/// logic.  Handler integration (actual DB retrieval) requires a running CMS
/// and is covered by staging verification steps documented at the bottom of
/// this file.
/// </summary>
[TestFixture]
public class GetSelectedExperiencesQueryTests
{
    private static readonly Guid GuidA = new("aaaaaaaa-0000-0000-0000-000000000000");
    private static readonly Guid GuidB = new("bbbbbbbb-0000-0000-0000-000000000000");
    private static readonly Guid GuidC = new("cccccccc-0000-0000-0000-000000000000");

    // -----------------------------------------------------------------------
    // Cache key (CacheValueKey) contract
    // -----------------------------------------------------------------------

    [Test]
    public void CacheValueKey_EmptyList_IsEmptyString()
    {
        var q = new GetSelectedExperiencesQuery("ETG", []);
        Assert.That(q.CacheValueKey, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CacheValueKey_SingleGuid_IsGuidString()
    {
        var q = new GetSelectedExperiencesQuery("ETG", [GuidA]);
        Assert.That(q.CacheValueKey, Is.EqualTo(GuidA.ToString()));
    }

    [Test]
    public void CacheValueKey_MultipleGuids_IsSortedAndJoined()
    {
        // Regardless of the insertion order the key must be order-independent
        // so that two requests with the same set map to the same cache entry.
        var ascending = new GetSelectedExperiencesQuery("ETG", [GuidA, GuidB, GuidC]);
        var descending = new GetSelectedExperiencesQuery("ETG", [GuidC, GuidB, GuidA]);
        var shuffled  = new GetSelectedExperiencesQuery("ETG", [GuidB, GuidA, GuidC]);

        Assert.That(ascending.CacheValueKey,  Is.EqualTo(descending.CacheValueKey));
        Assert.That(ascending.CacheValueKey,  Is.EqualTo(shuffled.CacheValueKey));
    }

    [Test]
    public void CacheValueKey_DifferentGuidSets_ProduceDifferentKeys()
    {
        var q1 = new GetSelectedExperiencesQuery("ETG", [GuidA, GuidB]);
        var q2 = new GetSelectedExperiencesQuery("ETG", [GuidA, GuidC]);

        Assert.That(q1.CacheValueKey, Is.Not.EqualTo(q2.CacheValueKey));
    }

    [Test]
    public void CacheValueKey_DifferentChannels_KeysAreDifferentViaQueryRecord()
    {
        // Channel name is part of the cache-entry name via ItemNameParts
        // (the base ContentItemQueryHandler uses IChannelContentQuery.ChannelName).
        // Here we verify that two queries with different channels are not equal
        // as records (which is what the pipeline uses to look them up).
        var q1 = new GetSelectedExperiencesQuery("ChannelA", [GuidA]);
        var q2 = new GetSelectedExperiencesQuery("ChannelB", [GuidA]);

        Assert.That(q1, Is.Not.EqualTo(q2));
        Assert.That(q1.ChannelName, Is.Not.EqualTo(q2.ChannelName));
    }

    // -----------------------------------------------------------------------
    // Guard: empty-list fast-path
    // -----------------------------------------------------------------------

    [Test]
    public void Query_WithZeroGuids_CacheValueKeyIsEmpty()
    {
        // Ensures the query is still a legal IQuery even with no GUIDs;
        // the handler returns [] immediately in that case.
        var q = new GetSelectedExperiencesQuery("ETG", []);
        Assert.That(q.CacheValueKey, Is.Empty);
        Assert.That(q.SelectedGuids, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // IChannelContentQuery contract
    // -----------------------------------------------------------------------

    [Test]
    public void IChannelContentQuery_ChannelName_MatchesConstructorArgument()
    {
        var q = new GetSelectedExperiencesQuery("MyChannel", [GuidA]);

        // Cast to the interface to verify the explicit implementation.
        ETG.Core.Operations.IChannelContentQuery iface = q;
        Assert.That(iface.ChannelName, Is.EqualTo("MyChannel"));
    }
}

/*
 * =========================================================================
 * STAGING VERIFICATION STEPS  (not yet executed — require a running CMS)
 * =========================================================================
 *
 * The following checks cannot be automated without a live database.  They
 * must be performed manually on a staging environment before shipping to
 * production.
 *
 * SV-1  COLD CACHE — EXPERIENCE QUERY SQL
 *   a. Enable XbyK SQL logging (CMSLogQueries=true in appsettings).
 *   b. Clear the in-process cache (recycle the app pool, or hit /CMSModules/Kentico.Content.Web.Mvc/api/Clear).
 *   c. Request the Home page.
 *   d. VERIFY: the Experience query contains WHERE WebPageItemGUID IN (...) with
 *      only the N selected GUIDs — not a full table scan.
 *   e. VERIFY: the row count returned equals the number of selected,
 *      published, language-available experiences (≤ N selected).
 *
 * SV-2  WARM CACHE — CACHE HIT
 *   a. Request the Home page a second time (same process, same culture).
 *   b. VERIFY: no Experience SQL query is logged — the progressive cache served
 *      the response.
 *
 * SV-3  CACHE INVALIDATION — EXPERIENCE CONTENT CHANGE
 *   a. In the CMS admin, rename an Experience page that is selected on the
 *      Home page and publish the change.
 *   b. Request the Home page.
 *   c. VERIFY: the new name is shown — the cache was invalidated by the
 *      webpageitem|bychannel|<channel>|bycontenttype|ETG.Experience dependency.
 *
 * SV-4  CACHE INVALIDATION — RE-PUBLISHING A MISSING EXPERIENCE
 *   a. Unpublish one of the selected Experience pages.
 *   b. Request the Home page — that tile should disappear.
 *   c. Re-publish the Experience page.
 *   d. Request the Home page — the tile should reappear immediately (before
 *      expiry), because the webpageitem|byguid|<guid> dependency on the
 *      missing page fires on publication.
 *
 * SV-5  EDITOR-SELECTED ORDER IS PRESERVED
 *   a. In the CMS admin, change the order of selected experiences on the
 *      Home page and publish.
 *   b. Clear the cache for the Home page (or wait for the homepage query
 *      dependency to fire).
 *   c. Request the Home page.
 *   d. VERIFY: the experiences render in the new order.
 *
 * SV-6  PREVIEW / PAGE BUILDER MODE
 *   a. Open the Home page in the Page Builder (IsPreview = true).
 *   b. VERIFY: the QueryCachingPipelineBehavior bypasses the cache (the pipeline
 *      short-circuits when isPreview = true), so the live database is always hit
 *      and unpublished/draft experiences are shown if appropriate.
 *
 * SV-7  MULTI-INSTANCE (AZURE) INVALIDATION
 *   a. If more than one App Service instance is running, make a content change
 *      on one instance.
 *   b. VERIFY: subsequent requests on the other instance also reflect the change.
 *   c. NOTE: XbyK's IProgressiveCache relies on the shared SQL cache-dependency
 *      mechanism (CacheDependencyAttribute / dummy keys written to the DB).  This
 *      works correctly across Azure instances as long as all instances share the
 *      same Azure SQL database — which is the standard XbyK deployment topology.
 *      No additional infrastructure change is needed.
 *
 * =========================================================================
 */
