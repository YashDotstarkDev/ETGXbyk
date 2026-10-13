using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.Testimonial;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: TestimonialWidget.IDENTIFIER,
    viewComponentType: typeof(TestimonialWidget),
    name: TestimonialWidget.NAME,
    propertiesType: typeof(TestimonialProperties),
    Description = "Displays a testimonial/quote section with author details.",
    IconClass = KenticoIcons.BUBBLE,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.Testimonial;

public class TestimonialWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.Testimonial";
    public const string NAME = "Testimonial";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(
        ComponentViewModel<TestimonialProperties> cvm)
    {
        var props = cvm.Properties;

        var imageReferences = props.Images?
            .Where(image => image.Identifier != Guid.Empty)
            .ToList() ?? [];

        Maybe<MediaFile> image = Maybe<MediaFile>.None;

        if (imageReferences.Count > 0)
        {
            image = await contentRetriever
                .RetrieveContentByGuids<MediaFile>(
                    imageReferences.Select(image => image.Identifier))
                .TryFirst();
        }

        return Validate(props, image)
            .Match(
                vm => View(
                    "~/Components/PageBuilder/Widgets/Testimonial/Testimonial.cshtml",
                    vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<TestimonialWidgetViewModel, ComponentErrorViewModel> Validate(
        TestimonialProperties props,
        Maybe<MediaFile> image)
    {
        ImageAssetViewModel? imageViewModel = null;

        if (image.TryGetValue(out var img) && img.LegacyMediaFileAsset is not null)
        {
            imageViewModel = ImageAssetViewModel.Create(img);
        }

        return new TestimonialWidgetViewModel(imageViewModel, props);
    }
}

[FormCategory(Label = "Content", Order = 1)]
public class TestimonialProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        MediaFile.CONTENT_TYPE_NAME,
        Label = "Image (1500 x 600px)",
        MaximumItems = 1,
        DefaultViewMode =
            Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 1)]
    public IEnumerable<ContentItemReference> Images { get; set; } = [];

    [TextInputComponent(Label = "Heading", Order = 2)]
    public string Heading { get; set; } = "";

    [TextAreaComponent(Label = "Text Content", Order = 3)]
    public string TextContent { get; set; } = "";

    [TextInputComponent(Label = "Date", Order = 4)]
    public string Date { get; set; } = "";

    [TextInputComponent(Label = "Source", Order = 5)]
    public string Source { get; set; } = "";

    [TextInputComponent(Label = "Source Location", Order = 6)]
    public string SourceLocation { get; set; } = "";

    [TextInputComponent(Label = "CTA Label", Order = 7)]
    public string CTALabel { get; set; } = "";

    [TextInputComponent(Label = "CTA Path", Order = 8)]
    public string Path { get; set; } = "";
}

public class TestimonialWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = TestimonialWidget.NAME;

    public ImageAssetViewModel? Image { get; }
    public string Heading { get; }
    public string TextContent { get; }
    public string Date { get; }
    public string Source { get; }
    public string SourceLocation { get; }
    public string CTALabel { get; }
    public string Path { get; }

    public TestimonialWidgetViewModel(
        ImageAssetViewModel? image,
        TestimonialProperties props)
    {
        Image = image;
        Heading = props.Heading;
        TextContent = props.TextContent;
        Date = props.Date;
        Source = props.Source;
        SourceLocation = props.SourceLocation;
        CTALabel = props.CTALabel;
        Path = props.Path;
    }
}