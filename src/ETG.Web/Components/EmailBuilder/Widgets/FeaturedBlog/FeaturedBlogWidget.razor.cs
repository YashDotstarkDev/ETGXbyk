using CMS.Base;
using CMS.ContentEngine;
using ETG.Core.Content;
using ETG.Web.Components.EmailBuilder.Widgets.FeaturedBlog;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Components;
using System.Linq; // Ensure System.Linq is present

[assembly: RegisterEmailWidget(
    identifier: FeaturedBlogWidget.IDENTIFIER,
    name: FeaturedBlogWidget.NAME,
    componentType: typeof(FeaturedBlogWidget),
    PropertiesType = typeof(FeaturedBlogWidgetProperties),
    IconClass = "icon-cogwheel-square",
    Description = "Featured Blog widget.")]

namespace ETG.Web.Components.EmailBuilder.Widgets.FeaturedBlog;

public partial class FeaturedBlogWidget : ComponentBase
{
    public const string IDENTIFIER = "ETG.Web.EmailWidget.FeaturedBlog";
    public const string NAME = "Featured Blog";

    [Inject]
    private IContentRetriever ContentRetriever { get; set; } = default!;

    [Parameter]
    public FeaturedBlogWidgetProperties Properties { get; set; } = new();

    protected FeaturedBlogWidgetModel Model { get; set; } = new();

    public override Task SetParametersAsync(ParameterView parameters) =>
        ContextUtils.PropagateCurrent(async () => await base.SetParametersAsync(parameters))();

    protected override async Task OnInitializedAsync()
    {
        // FIX: Target the collection inside the properties object
        var ImageGuid = Properties.Image.FirstOrDefault()?.Identifier ?? Guid.Empty;

        var guids = new[] { ImageGuid }.Where(g => g != Guid.Empty).Distinct().ToList();

        var mediaDict = new Dictionary<Guid, MediaFile>();
        if (guids.Count > 0)
        {
            var files = await ContentRetriever.RetrieveContentByGuids<MediaFile>(guids);
            mediaDict = files.ToDictionary(f => f.SystemFields.ContentItemGUID);
        }

        string ImageUrl = string.Empty;
        if (ImageGuid != Guid.Empty && mediaDict.TryGetValue(ImageGuid, out var mf_Image))
            ImageUrl = mf_Image.LegacyMediaFileAsset?.Url ?? string.Empty;

        Model = new FeaturedBlogWidgetModel
        {
            MainHeading = Properties.MainHeading,
            Image = ImageUrl,
            Title = Properties.Title,
            SubTitle = Properties.SubTitle,
            Description = Properties.Description,
            CTALabel = Properties.CTALabel,
            CTAUrl = Properties.CTAUrl,
        };
    }
}

public class FeaturedBlogWidgetProperties : IEmailWidgetProperties
{
    [TextInputComponent(Label = "Main Heading", Order = 1)]
    public string MainHeading { get; set; } = string.Empty;

    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image",
        MaximumItems = 1,
        DefaultViewMode = ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> Image { get; set; } = [];

    [TextInputComponent(Label = "Title", Order = 3)]
    public string Title { get; set; } = string.Empty;

    [TextInputComponent(Label = "Sub Title", Order = 4)]
    public string SubTitle { get; set; } = string.Empty;

    [TextAreaComponent(Label = "Description", Order = 5)]
    public string Description { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Label", Order = 6)]
    public string CTALabel { get; set; } = string.Empty;

    [TextInputComponent(Label = "CTA Url", Order = 7)]
    public string CTAUrl { get; set; } = string.Empty;
}

public class FeaturedBlogWidgetModel
{
    public string MainHeading { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SubTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CTALabel { get; set; } = string.Empty;
    public string CTAUrl { get; set; } = string.Empty;
}
