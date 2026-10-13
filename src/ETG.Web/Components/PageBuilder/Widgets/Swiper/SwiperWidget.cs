using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Components.PageBuilder.Widgets.Hero;
using ETG.Web.Components.PageBuilder.Widgets.Swiper;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: SwiperWidget.IDENTIFIER,
    viewComponentType: typeof(SwiperWidget),
    name: SwiperWidget.NAME,
    propertiesType: typeof(SwiperWidgetProperties),
    Description = "Displays a configurable Swiper.js slider",
    IconClass = KenticoIcons.PICTURE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.Swiper;

public class SwiperWidget(IContentRetriever contentRetriever, ContentItemQueryTools tools, IWebPageUrlRetriever urlRetriever) : ViewComponent
{
    public const string IDENTIFIER = "Website.SwiperWidget";
    public const string NAME = "Swiper Slider";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly ContentItemQueryTools tools = tools;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<SwiperWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var (slides, group) = props.DataSourceParsed switch
        {
            SwiperDataSources.Group => await GetSlidesFromGroup(props),
            SwiperDataSources.Individual => (await GetSlidesBySelection(props), Maybe<SwiperSlidesGroup>.None)
        };

        var validation = Validate(slides, group, props);
        if (validation.IsFailure)
        {
            return View("~/Components/ComponentError.cshtml", validation.Error);
        }

        // Map content slides to view DTOs and resolve CTA links
        var slideDtos = new List<SwiperSlideItem>();

        foreach (var s in slides)
        {
            var desktopUrl = s.DesktopBackgroundImage?.FirstOrDefault();
            var mobileUrl = s.MobileBackgroundImage?.FirstOrDefault();

            string ctaUrl = "#";
            var linkItem = s.CTALink?.FirstOrDefault();
            if (linkItem?.WebPageGuid != Guid.Empty)
            {
                var guids = new List<Guid> { linkItem.WebPageGuid };
                var urls = await urlRetriever.Retrieve(guids, tools.WebsiteChannelContext.WebsiteChannelName, "en", tools.WebsiteChannelContext.IsPreview, CancellationToken.None);
                if (urls != null && urls.TryGetValue(linkItem.WebPageGuid, out var pageUrl))
                {
                    ctaUrl = pageUrl.RelativePath ?? "#";
                }
            }

            slideDtos.Add(new SwiperSlideItem
            {
                LegacyMediaFileTitle = s.BasicItemTitle,
                BasicItemShortDescription = s.BasicItemShortDescription,
                DesktopUrl = desktopUrl != null ? ImageAssetViewModel.Create(desktopUrl) : null,
                MobileUrl = mobileUrl != null ? ImageAssetViewModel.Create(mobileUrl) : null,
                CTAText = s.CTAText,
                CTAUrl = ctaUrl,
                OpenInNewTab = s.OpenInNewTab,
                CustomCSSClass = s.CustomCSSClass
            });
        }

        return View("~/Components/PageBuilder/Widgets/Swiper/Swiper.cshtml", new SwiperWidgetViewModel(props, slideDtos, group));
    }

    private async Task<IReadOnlyList<SwiperSlide>> GetSlidesBySelection(SwiperWidgetProperties props)
    {
        var guids = (props.SwiperSlides ?? []).Select(i => i.Identifier).ToArray();

        if (guids.Length == 0) return [];

        var items = (await contentRetriever
                .RetrieveContentByGuids<SwiperSlide>(guids, new RetrieveContentParameters { LinkedItemsMaxLevel = 1 }))
            .OrderBy(item => Array.IndexOf(guids, item.SystemFields.ContentItemGUID))
            .ToList();
        return items;
    }

    private async Task<(IReadOnlyList<SwiperSlide> items, Maybe<SwiperSlidesGroup> group)> GetSlidesFromGroup(SwiperWidgetProperties props)
    {
        var groupGuid = (props.SwiperGroup ?? []).Select(i => i.Identifier).TryFirst();

        if (!groupGuid.TryGetValue(out var guid)) return ([], Maybe<SwiperSlidesGroup>.None);

        var group = await contentRetriever
            .RetrieveContentByGuids<SwiperSlidesGroup>([guid], new RetrieveContentParameters { LinkedItemsMaxLevel = 2 })
            .TryFirst();

        if (!group.TryGetValue(out var grp)) return ([], Maybe<SwiperSlidesGroup>.None);

        var items = grp.SwiperSlidesGroupSwiperSlide.ToList();
        return (items, grp);
    }

    private Result<bool, ComponentErrorViewModel> Validate(
        IReadOnlyList<SwiperSlide> slides,
        Maybe<SwiperSlidesGroup> group,
        SwiperWidgetProperties props)
    {
        if (slides.Count == 0)
        {
            string err = props.DataSourceParsed == SwiperDataSources.Group
                ? "Select a Swiper slides group or ensure the selected group contains slides."
                : "Select at least 1 Swiper slide.";

            return Result.Failure<bool, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, err));
        }

        return Result.Success<bool, ComponentErrorViewModel>(true);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class SwiperWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(Label = "Label", ExplanationText = "Optional heading displayed above the slider", Order = 1)]
    public string Label { get; set; } = "";

    [DropDownComponent(Label = "Data Source", DataProviderType = typeof(EnumDropDownOptionsProvider<SwiperDataSources>), Order = 2)]
    public string DataSource { get; set; } = nameof(SwiperDataSources.Individual);
    public SwiperDataSources DataSourceParsed => EnumDropDownOptionsProvider<SwiperDataSources>.Parse(DataSource, SwiperDataSources.Individual);

    [ContentItemSelectorComponent(contentTypeName: "Website.SwiperSlide", Label = "Slides", AllowContentItemCreation = true, Order = 3)]
    [VisibleIfEqualTo(nameof(DataSource), nameof(SwiperDataSources.Individual), StringComparison.OrdinalIgnoreCase)]
    public IEnumerable<ContentItemReference> SwiperSlides { get; set; } = [];

    [ContentItemSelectorComponent(contentTypeName: "Website.SwiperSlidesGroup", Label = "Slides Group", AllowContentItemCreation = true, MaximumItems = 1, Order = 3)]
    [VisibleIfEqualTo(nameof(DataSource), nameof(SwiperDataSources.Group), StringComparison.OrdinalIgnoreCase)]
    public IEnumerable<ContentItemReference> SwiperGroup { get; set; } = [];

    [DropDownComponent(Label = "Padding", Tooltip = "Select a padding size applied to both top and bottom", DataProviderType = typeof(EnumDropDownOptionsProvider<MarginSizes>), Order = 4)]
    public string Padding { get; set; } = nameof(MarginSizes.Medium);
    public MarginSizes PaddingParsed => EnumDropDownOptionsProvider<MarginSizes>.Parse(Padding, MarginSizes.Medium);

    // Swiper settings
    [CheckBoxComponent(Label = "Loop", Order = 10)]
    public bool Loop { get; set; } = true;

    [NumberInputComponent(Label = "Initial Slide (index)", Order = 11)]
    public int InitialSlide { get; set; } = 0;

    [NumberInputComponent(Label = "Slides Per View", ExplanationText = "Number of slides visible in view", Order = 12)]
    public int SlidesPerView { get; set; } = 1;

    [NumberInputComponent(Label = "Space Between (px)", ExplanationText = "Space between slides in px", Order = 13)]
    public int SpaceBetween { get; set; } = 0;

    [CheckBoxComponent(Label = "Center Slides", Order = 14)]
    public bool CenteredSlides { get; set; } = false;

    [CheckBoxComponent(Label = "Autoplay", Order = 15)]
    public bool Autoplay { get; set; } = false;

    [NumberInputComponent(Label = "Autoplay Delay (ms)", Order = 16)]
    //[VisibleIfEqualTo(nameof(Autoplay), nameof(Autoplay), StringComparison.OrdinalIgnoreCase)]
    public int AutoplayDelay { get; set; } = 5000;

    [CheckBoxComponent(Label = "Enable Navigation (prev/next)", Order = 17)]
    public bool Navigation { get; set; } = true;

    [CheckBoxComponent(Label = "Enable Pagination (bullets)", Order = 18)]
    public bool Pagination { get; set; } = false;

    [DropDownComponent(Label = "Pagination Type", DataProviderType = typeof(EnumDropDownOptionsProvider<SwiperPaginationType>), Order = 19)]
    //[VisibleIfEqualTo(nameof(Pagination), nameof(Pagination), StringComparison.OrdinalIgnoreCase)]
    public string PaginationType { get; set; } = nameof(SwiperPaginationType.Bullets);
    public SwiperPaginationType PaginationTypeParsed => EnumDropDownOptionsProvider<SwiperPaginationType>.Parse(PaginationType, SwiperPaginationType.Bullets);

    [CheckBoxComponent(Label = "Enable Scrollbar", Order = 20)]
    public bool Scrollbar { get; set; } = false;

    [CheckBoxComponent(Label = "Draggable Scrollbar", Order = 21)]
    //[VisibleIfEqualTo(nameof(Scrollbar), nameof(Scrollbar), StringComparison.OrdinalIgnoreCase)]
    public bool ScrollbarDraggable { get; set; } = true;

    [CheckBoxComponent(Label = "Keyboard Control", Order = 22)]
    public bool Keyboard { get; set; } = false;

    [CheckBoxComponent(Label = "Mousewheel Control", Order = 23)]
    public bool Mousewheel { get; set; } = false;

    [TextInputComponent(Label = "Effect", ExplanationText = "Slide, fade, cube, coverflow, flip", Order = 24)]
    public string Effect { get; set; } = "slide";

    [TextInputComponent(Label = "Custom CSS Class", Order = 25)]
    public string CustomCssClass { get; set; } = "";
}
public class SwiperWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = SwiperWidget.NAME;

    public string Label { get; }
    public string DisplayLabel { get; }
    public bool HasLabel { get; }
    public IReadOnlyList<SwiperSlideItem> Slides { get; }
    public bool IsFromGroup { get; }
    public string? GroupTitle { get; }
    public string? GroupDescription { get; }
    public Guid WidgetInstanceID { get; }

    // settings
    public bool Loop { get; }
    public int InitialSlide { get; }
    public int SlidesPerView { get; }
    public int SpaceBetween { get; }
    public bool CenteredSlides { get; }
    public bool Autoplay { get; }
    public int AutoplayDelay { get; }
    public bool Navigation { get; }
    public bool Pagination { get; }
    public SwiperPaginationType PaginationType { get; }
    public bool Scrollbar { get; }
    public bool ScrollbarDraggable { get; }
    public bool Keyboard { get; }
    public bool Mousewheel { get; }
    public string Effect { get; }
    public string CustomCssClass { get; }
    public MarginSizes Padding { get; }

    public SwiperWidgetViewModel(SwiperWidgetProperties props, IReadOnlyList<SwiperSlideItem> slides, Maybe<SwiperSlidesGroup> group)
    {
        Label = props.Label;
        Slides = slides;
        WidgetInstanceID = props.ID;
        IsFromGroup = props.DataSourceParsed == SwiperDataSources.Group;
        GroupTitle = group.TryGetValue(out var grp) ? grp.BasicItemTitle : null;
        GroupDescription = group.TryGetValue(out var grp2) ? grp2.BasicItemShortDescription : null;

        DisplayLabel = IsFromGroup && !string.IsNullOrWhiteSpace(GroupTitle) ? GroupTitle : Label;
        HasLabel = !string.IsNullOrWhiteSpace(DisplayLabel);

        // settings
        Loop = props.Loop;
        InitialSlide = props.InitialSlide;
        SlidesPerView = Math.Max(1, props.SlidesPerView);
        SpaceBetween = Math.Max(0, props.SpaceBetween);
        CenteredSlides = props.CenteredSlides;
        Autoplay = props.Autoplay;
        AutoplayDelay = props.AutoplayDelay;
        Navigation = props.Navigation;
        Pagination = props.Pagination;
        PaginationType = props.PaginationTypeParsed;
        Scrollbar = props.Scrollbar;
        ScrollbarDraggable = props.ScrollbarDraggable;
        Keyboard = props.Keyboard;
        Mousewheel = props.Mousewheel;
        Effect = string.IsNullOrWhiteSpace(props.Effect) ? "slide" : props.Effect;
        CustomCssClass = props.CustomCssClass;
        Padding = props.PaddingParsed;
    }
}

public class SwiperSlideItem
{
    public string LegacyMediaFileTitle { get; set; }
    public string BasicItemShortDescription { get; set; }
    public ImageAssetViewModel DesktopUrl { get; set; }
    public ImageAssetViewModel MobileUrl { get; set; }
    public string CTAText { get; set; }
    public string CTAUrl { get; set; }
    public bool OpenInNewTab { get; set; }
    public string CustomCSSClass { get; set; }
}
public enum SwiperDataSources { [Description("Individual Slides")] Individual, [Description("Slides Group")] Group }
public enum SwiperPaginationType { Bullets, Fraction, Progressbar }
