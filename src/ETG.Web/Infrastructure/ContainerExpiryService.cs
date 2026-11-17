using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG.Core.Content;
using Kentico.Content.Web.Mvc;
using Microsoft.Extensions.Logging;

namespace ETG.Web.Infrastructure;

public interface IContainerExpiryService
{
    Task<string?> GetExpiryRedirectUrlAsync(
        Container? container,
        CancellationToken cancellationToken = default);
}

public sealed class ContainerExpiryService(
    IWebPageUrlRetriever urlRetriever,
    IContentQueryExecutor contentQueryExecutor,
    IWebsiteChannelContext websiteChannelContext,
    ILogger<ContainerExpiryService> logger
) : IContainerExpiryService
{
    public async Task<string?> GetExpiryRedirectUrlAsync(
        Container? container,
        CancellationToken cancellationToken = default)
    {
        if (container is null)
        {
            return null;
        }

        // Both fields empty or expiration date not set: page works as it is.
        if (container.ContainerExpiry <= DateTimeHelper.ZERO_TIME ||
            container.ContainerExpiry == DateTime.MinValue)
        {
            return null;
        }

        var now = container.ContainerExpiry.Kind == DateTimeKind.Utc
            ? DateTime.UtcNow
            : DateTime.Now;

        // If expiration date is in the future, page has not expired.
        if (now < container.ContainerExpiry)
        {
            return null;
        }

        // Page is expired (date is past or present).
        // First try to resolve from preloaded ContainerRedirect.
        var redirectPage = container.ContainerRedirect?
            .OfType<IWebPageFieldsSource>()
            .FirstOrDefault(target =>
                target?.SystemFields is not null &&
                target.SystemFields.WebPageItemWebsiteChannelId > 0);

        // Fallback: If ContainerRedirect was not preloaded with linked web page data,
        // query Kentico for the redirect target using the container's WebPageItemID.
        if (redirectPage is null &&
            container.SystemFields is not null &&
            container.SystemFields.WebPageItemID > 0 &&
            !string.IsNullOrWhiteSpace(websiteChannelContext.WebsiteChannelName))
        {
            try
            {
                var redirectQuery = new ContentItemQueryBuilder()
                    .ForContentType(
                        Container.CONTENT_TYPE_NAME,
                        parameters =>
                        {
                            parameters.ForWebsite(websiteChannelContext.WebsiteChannelName);
                            parameters.Where(where => where.WhereEquals(
                                nameof(WebPageFields.WebPageItemID),
                                container.SystemFields.WebPageItemID));
                            parameters.WithLinkedItems(
                                1,
                                options => options.IncludeWebPageData());
                        });

                var redirectContainers = await contentQueryExecutor.GetMappedWebPageResult<Container>(
                    redirectQuery,
                    cancellationToken: cancellationToken);

                var loadedContainer = redirectContainers?.FirstOrDefault();
                if (loadedContainer?.ContainerRedirect is not null)
                {
                    container.ContainerRedirect = loadedContainer.ContainerRedirect;

                    redirectPage = loadedContainer.ContainerRedirect
                        .OfType<IWebPageFieldsSource>()
                        .FirstOrDefault(target =>
                            target?.SystemFields is not null &&
                            target.SystemFields.WebPageItemWebsiteChannelId > 0);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to query ContainerRedirect for Container WebPageItemID {WebPageItemID}",
                    container.SystemFields.WebPageItemID);
            }
        }

        if (redirectPage is null)
        {
            return null;
        }

        // Prevent self-redirect loops if an editor accidentally selected the same page
        if (container.SystemFields is not null &&
            redirectPage.SystemFields is not null &&
            redirectPage.SystemFields.WebPageItemID == container.SystemFields.WebPageItemID)
        {
            logger.LogWarning(
                "Container '{ContainerName}' (ID {WebPageItemID}) is configured to redirect to itself upon expiry; ignoring.",
                container.ContainerName,
                container.SystemFields.WebPageItemID);
            return null;
        }

        try
        {
            var redirectUrl = await urlRetriever.Retrieve(redirectPage);
            if (!string.IsNullOrWhiteSpace(redirectUrl?.RelativePath))
            {
                return redirectUrl.RelativePath;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to retrieve redirect URL for Container '{ContainerName}'",
                container.ContainerName);
        }

        return null;
    }
}

