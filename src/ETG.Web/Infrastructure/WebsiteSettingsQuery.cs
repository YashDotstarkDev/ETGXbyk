using CMS.ContentEngine;
using CMS.DataEngine;
using ETG.Core.Content;
using ETG.Core.Modules;
using ETG.Core.Operations;

namespace ETG.Web.Infrastructure;

public record WebsiteSettingsQuery : IQuery<WebsiteSettingsQueryResponse>;
public record WebsiteSettingsQueryResponse(
    WebsiteSettingsInfo WebsiteSettings,
    WebsiteGlobalContent GlobalContent);
public class WebsiteSettingsQueryHandler(ContentItemQueryTools tools, IInfoProvider<WebsiteSettingsInfo> settingsProvider) : ContentItemQueryHandler<WebsiteSettingsQuery, WebsiteSettingsQueryResponse>(tools)
{
    private readonly IInfoProvider<WebsiteSettingsInfo> settingsProvider = settingsProvider;

    public override async Task<WebsiteSettingsQueryResponse> Handle(WebsiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider
            .Get()
            .TopN(1)
            .FirstOrDefaultAsync() ?? throw new Exception($"A valid {WebsiteSettingsInfo.OBJECT_TYPE} record is required by the application.");

        var globalContent = await GetGlobalContent(settings);

        return new(settings, globalContent);
    }

    private async Task<WebsiteGlobalContent> GetGlobalContent(WebsiteSettingsInfo websiteSettings)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                WebsiteGlobalContent.CONTENT_TYPE_NAME,
                p => p
                    .Where(w => w.WhereContentItem(websiteSettings.WebsiteSettingsWebsiteGlobalContent.Select(c => c.Identifier).FirstOrDefault()))
                    .WithLinkedItems(1));
        var contents = await Executor.GetMappedResult<WebsiteGlobalContent>(b, DefaultQueryOptions);

        return contents.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(WebsiteSettingsQuery query, WebsiteSettingsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
      builder.AllObjects(WebsiteSettingsInfo.OBJECT_TYPE)
          .Collection(result.GlobalContent.FooterLogo, (c, b) => b.ContentItem(c))
          .AllContentItems(WebsiteGlobalContent.CONTENT_TYPE_NAME);
}