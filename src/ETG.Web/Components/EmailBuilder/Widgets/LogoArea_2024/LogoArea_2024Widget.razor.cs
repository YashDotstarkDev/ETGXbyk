using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.LogoArea_2024;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using System.Linq; // Ensure System.Linq is present

[assembly: RegisterEmailWidget(
    identifier: LogoArea_2024Widget.IDENTIFIER,
    name: LogoArea_2024Widget.NAME,
    componentType: typeof(LogoArea_2024Widget),
    PropertiesType = typeof(LogoArea_2024WidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Logo Area (2024) widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.LogoArea_2024;

public partial class LogoArea_2024Widget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.LogoArea_2024";
    public const string NAME = "Logo Area (2024)";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public LogoArea_2024WidgetProperties Properties { get; set; } = new();

    protected LogoArea_2024WidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Properties ??= new LogoArea_2024WidgetProperties();

        var ImageGuid = Properties?.Image?.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { ImageGuid }.Where(g => g != Guid.Empty).Distinct().ToList();

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

        string ImageUrl = string.Empty;
        if (ImageGuid != Guid.Empty && mediaDict.TryGetValue(ImageGuid, out var mf_Image))
            ImageUrl = mf_Image.LegacyMediaFileAsset?.Url ?? string.Empty;

        Model = new LogoArea_2024WidgetModel
        {
            Heading = Properties?.Heading ?? string.Empty,
            Image = ImageUrl,
        };
    }
}

public class LogoArea_2024WidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Heading", Order = 1)]
    public string Heading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];
}

public class LogoArea_2024WidgetModel
{
    public string Heading { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
