using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using ETG;
using ETG.Core.Content;
using ETG.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace ETG.Web.Features.Payment.Operations;

public record PaymentPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<PaymentPageQueryResponse>(Page);

public record PaymentPageQueryResponse(PaymentPage? Page, IEnumerable<FAQ> FAQs);

public class PaymentPageQueryHandler(WebPageQueryTools tools)
    : WebPageQueryHandler<PaymentPageQuery, PaymentPageQueryResponse>(tools)
{
    public override async Task<PaymentPageQueryResponse> Handle(
        PaymentPageQuery request,
        CancellationToken cancellationToken = default)
    {
        // 1. Retrieve the PaymentPage web page item with linked items
        var pageQuery = new ContentItemQueryBuilder()
            .ForWebPage(request.Page, config => config.WithLinkedItems(2));

        var pageResult = await Executor.GetMappedWebPageResult<PaymentPage>(
            pageQuery,
            DefaultQueryOptions,
            cancellationToken);

        var paymentPage = pageResult.FirstOrDefault();

        // 2. Retrieve Payment FAQs from /Shared/Payment-FAQs folder
        var faqs = new List<FAQ>();
        try
        {
            string channelName = !string.IsNullOrWhiteSpace(request.Page.WebsiteChannelName)
                ? request.Page.WebsiteChannelName
                : "ETG";

            var faqFolderQuery = new ContentItemQueryBuilder()
                .ForContentType(
                    Folder.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(
                            channelName,
                            pathMatch: PathMatch.Single("/Shared/Payment-FAQs"))
                        .WithLinkedItems(2));

            var folderResult = await Executor.GetMappedWebPageResult<Folder>(
                faqFolderQuery,
                DefaultQueryOptions,
                cancellationToken);

            var faqFolder = folderResult.FirstOrDefault();
            if (faqFolder?.SelectedFAQs != null)
            {
                faqs.AddRange(faqFolder.SelectedFAQs.Where(f => f != null));
            }
        }
        catch
        {
            // Fallback: If folder query encounters an issue, return empty FAQs without failing the page
        }

        return new PaymentPageQueryResponse(paymentPage, faqs);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        PaymentPageQuery query,
        PaymentPageQueryResponse result,
        ICacheDependencyKeysBuilder builder)
    {
        builder = base.AddDependencyKeys(query, result, builder);

        if (result?.Page != null)
        {
            builder.ContentItem(result.Page);
        }

        if (result?.FAQs != null)
        {
            foreach (var faq in result.FAQs)
            {
                builder.ContentItem(faq);
            }
        }

        return builder;
    }
}

