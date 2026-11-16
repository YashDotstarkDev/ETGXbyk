using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CMS.Membership;
using ETG.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace ETG.Web.Controllers;

/// <summary>
/// TEMPORARY endpoint for the one-time Tour vanity URL backfill.
/// DELETE THIS FILE after the backfill is complete.
/// Auth: header "X-Backfill-Secret" must equal env var TOUR_BACKFILL_SECRET.
/// Wrong or missing secret returns 404 so the endpoint is not discoverable.
/// </summary>
[IgnoreAntiforgeryToken]
[Route("ops/tour-backfill")] // not under /admin, which belongs to the Kentico admin UI
public class AdminTourBackfillController(
    TourVanityUrlBackfillRunner runner,
    IUserInfoProvider userInfoProvider) : Controller
{
    private const int AdminUserId = 53; // Change to a real, enabled administrator user ID

    [HttpGet("status")]
    public IActionResult Status() =>
        !IsAuthorized()
            ? NotFound()
            : Ok(new
            {
                AdminUserId,
                AdminUser = userInfoProvider.Get(AdminUserId)?.UserName ?? "NOT FOUND"
            });

    [HttpGet("dry-run")]
    public Task<IActionResult> DryRun(int skip = 0, int take = 100, CancellationToken ct = default) =>
        Run(TourBackfillMode.DryRun, skip, take, ct);

    [HttpPost("apply")]
    public Task<IActionResult> Apply(int skip = 0, int take = 100, CancellationToken ct = default) =>
        Run(TourBackfillMode.Apply, skip, take, ct);

    private async Task<IActionResult> Run(TourBackfillMode mode, int skip, int take, CancellationToken ct)
    {
        if (!IsAuthorized())
        {
            return NotFound();
        }

        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, 500);

        var report = await runner.RunAsync(mode, AdminUserId, skip, take, ct);

        Response.Headers["X-Total"] = report.Total.ToString();
        Response.Headers["X-Changed"] = report.Changed.ToString();
        Response.Headers["X-Skipped"] = report.Skipped.ToString();
        Response.Headers["X-Failed"] = report.Failures.Count.ToString();
        Response.Headers["X-Next-Skip"] = (skip + take).ToString();
        Response.Headers["X-Done"] = report.Done.ToString();

        return File(
            Encoding.UTF8.GetBytes(runner.GenerateCsv(report)),
            "text/csv",
            $"tour-backfill-{mode}-{skip}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private bool IsAuthorized()
    {
        var expected = Environment.GetEnvironmentVariable("TOUR_BACKFILL_SECRET");
        var given = Request.Headers["X-Backfill-Secret"].ToString();

        return !string.IsNullOrEmpty(expected) &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(expected),
                   Encoding.UTF8.GetBytes(given));
    }
}
