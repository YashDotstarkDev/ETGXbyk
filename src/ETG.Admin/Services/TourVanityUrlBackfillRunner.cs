using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;

namespace ETG.Admin.Services;

public sealed class TourVanityUrlBackfillRunner(
    TourVanityUrlService service,
    IContentQueryExecutor queryExecutor,
    IInfoProvider<ContentLanguageInfo> languageProvider,
    IWebsiteChannelContext channelContext)
{
    // Processes one slice (skip/take) so each HTTP request finishes within the timeout.
    // Safe to re-run: tours that already have the correct URL are skipped.
    public async Task<TourBackfillReport> RunAsync(
        TourBackfillMode mode, int userId, int skip, int take, CancellationToken ct)
    {
        var all = await GetAllToursAsync(ct);

        var changed = 0;
        var skipped = 0;
        var items = new List<TourBackfillItem>();
        var failures = new List<TourBackfillFailure>();

        foreach (var (id, lang) in all.Skip(skip).Take(take))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var r = await service.ProcessAsync(
                    channelContext.WebsiteChannelID, userId, id, lang, mode == TourBackfillMode.Apply, ct);

                if (r.Status == TourVanityUrlStatus.Failed)
                {
                    failures.Add(new TourBackfillFailure(id, lang, r.Message ?? "Failed"));
                    continue;
                }

                if (r.Status == TourVanityUrlStatus.Unchanged)
                {
                    skipped++;
                }
                else
                {
                    changed++;
                }

                items.Add(new TourBackfillItem(
                    id, lang, r.TourName, r.CurrentUrl ?? "", r.ComputedUrl ?? "", r.Rule.ToString(), r.Status.ToString()));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(new TourBackfillFailure(id, lang, ex.Message));
            }
        }

        return new TourBackfillReport(mode, all.Count, skip, take, changed, skipped, items, failures);
    }

    public string GenerateCsv(TourBackfillReport report)
    {
        var sb = new StringBuilder("PageID,Language,TourName,CurrentUrl,ComputedUrl,Rule,Status\n");

        foreach (var i in report.Items)
        {
            sb.Append($"{i.PageId},{i.Language},{Escape(i.TourName)},{Escape(i.CurrentUrl)},{Escape(i.ComputedUrl)},{i.Rule},{i.Status}\n");
        }

        if (report.Failures.Count > 0)
        {
            sb.Append("\nFAILURES\nPageID,Language,Error\n");

            foreach (var f in report.Failures)
            {
                sb.Append($"{f.PageId},{f.Language},{Escape(f.ErrorMessage)}\n");
            }
        }

        return sb.ToString();
    }

    // Every published Tour in every configured content language, in a stable order.
    private async Task<List<(int Id, string Lang)>> GetAllToursAsync(CancellationToken ct)
    {
        var result = new List<(int, string)>();

        foreach (var lang in languageProvider.Get().Select(l => l.ContentLanguageName).ToList())
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(Tour.CONTENT_TYPE_NAME, q => q.ForWebsite(channelContext.WebsiteChannelName))
                .InLanguage(lang, false);

            var tours = await queryExecutor.GetMappedWebPageResult<Tour>(query, cancellationToken: ct);
            result.AddRange(tours.Select(t => (t.SystemFields.WebPageItemID, lang)));
        }

        return result.OrderBy(x => x.Item1).ThenBy(x => x.Item2, StringComparer.Ordinal).ToList();
    }

    private static string Escape(string v)
    {
        // Prevent spreadsheet formula injection from editor-entered names.
        if (v.Length > 0 && "=+-@".Contains(v[0]))
        {
            v = "'" + v;
        }

        return v.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
    }
}

public enum TourBackfillMode
{
    DryRun,
    Apply
}

public record TourBackfillReport(
    TourBackfillMode Mode,
    int Total,
    int Skip,
    int Take,
    int Changed,
    int Skipped,
    List<TourBackfillItem> Items,
    List<TourBackfillFailure> Failures)
{
    public bool Done => Skip + Take >= Total;
}

public record TourBackfillItem(
    int PageId, string Language, string TourName,
    string CurrentUrl, string ComputedUrl, string Rule, string Status);

public record TourBackfillFailure(int PageId, string Language, string ErrorMessage);
