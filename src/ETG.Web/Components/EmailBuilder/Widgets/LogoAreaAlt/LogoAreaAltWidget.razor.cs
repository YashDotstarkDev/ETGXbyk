using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.LogoAreaAlt;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using System.Linq; // Ensure System.Linq is present

[assembly: RegisterEmailWidget(
    identifier: LogoAreaAltWidget.IDENTIFIER,
    name: LogoAreaAltWidget.NAME,
    componentType: typeof(LogoAreaAltWidget),
    PropertiesType = typeof(LogoAreaAltWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Logo Area (ALT) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.LogoAreaAlt;

public partial class LogoAreaAltWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.LogoAreaAlt";
    public const string NAME = "Logo Area (ALT)";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public LogoAreaAltWidgetProperties Properties { get; set; } = new();

    protected LogoAreaAltWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new LogoAreaAltWidgetProperties();

        var LogoGuid = Properties?.Logo?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { LogoGuid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            if (files is not null)
            {
                foreach (var file in files)
                {
                    var guid = file?.SystemFields?.ContentItemGUID ?? Guid.Empty;
                    if (file is not null && guid != Guid.Empty)
                    {
                        mediaDict.TryAdd(guid, file);
                    }
                }
            }
        }

        string LogoUrl = string.Empty;
        if (LogoGuid != Guid.Empty && mediaDict.TryGetValue(LogoGuid, out var mf_Logo))
            LogoUrl = mf_Logo.LegacyMediaFileAsset?.Url ?? string.Empty;

        Model = new LogoAreaAltWidgetModel
        {
            Logo = LogoUrl,
            copy = Properties?.copy ?? string.Empty,
        };
    }
}

public class LogoAreaAltWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Logo",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Logo { get; set; } = [];

    [TextAreaComponent(Label = "Copy", Order = 2)]
    public string copy { get; set; } = string.Empty;
}

public class LogoAreaAltWidgetModel
{
    public string Logo { get; set; } = string.Empty;
    public string copy { get; set; } = string.Empty;
}
