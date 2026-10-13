using CMS.ContentEngine;
using CSharpFunctionalExtensions;
using ETG.Core.Content;
using ETG.Web.Components.PageBuilder.Widgets.FourColumnIconCardGrid;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: FourColumnIconCardGridWidget.IDENTIFIER,
    viewComponentType: typeof(FourColumnIconCardGridWidget),
    name: FourColumnIconCardGridWidget.NAME,
    propertiesType: typeof(FourColumnIconCardGridWidgetProperties),
    Description = "Displays a four-column grid of icon cards with heading, description, and footer text.",
    IconClass = KenticoIcons.L_GRID_2_2,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.FourColumnIconCardGrid;

public class FourColumnIconCardGridWidget(IContentRetriever contentRetriever) : ViewComponent
{
    public const string IDENTIFIER = "ETG.Web.Widget.FourColumnIconCardGrid";
    public const string NAME = "Four Column Icon Card Grid";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FourColumnIconCardGridWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var cardGuids = props.FeaturesCards
            .Select(card => card.Identifier)
            .ToArray();

        var cards = await contentRetriever.RetrieveContentByGuids<Card>(cardGuids,
            new RetrieveContentParameters
            {
                LinkedItemsMaxLevel = 1
            });

        return Validate(props, cards)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/FourColumnIconCardGrid/FourColumnIconCardGrid.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private static Result<FourColumnIconCardGridWidgetViewModel, ComponentErrorViewModel> Validate(FourColumnIconCardGridWidgetProperties props, IEnumerable<Card> cards)
    {
        if (props.FeaturesCards is null || !props.FeaturesCards.Any())
        {
            return Result.Failure<FourColumnIconCardGridWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No feature cards have been selected."));
        }

        if (cards is null || !cards.Any())
        {
            return Result.Failure<FourColumnIconCardGridWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected card content items no longer exist."));
        }

        return new FourColumnIconCardGridWidgetViewModel(props, cards);
    }
}


[FormCategory(Label = "Content", Order = 1)]
public class FourColumnIconCardGridWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Heading",
        Order = 1)]
    public string Heading { get; set; } = "";

    [TextAreaComponent(
        Label = "Short Description",
        Order = 2)]
    public string ShortDescription { get; set; } = "";

    [ContentItemSelectorComponent(
        Card.CONTENT_TYPE_NAME,
        Label = "Features Cards",
        MinimumItems = 1,
        MaximumItems = 4,
        DefaultViewMode = Kentico.Xperience.Admin.Base.Forms.ContentItemSelectorViewMode.Grid,
        AllowContentItemCreation = true,
        Order = 3)]
    public IEnumerable<ContentItemReference> FeaturesCards { get; set; } = [];

    [TextAreaComponent(
        Label = "Footer Text",
        Order = 4)]
    public string FooterText { get; set; } = "";
}

public class FourColumnIconCardGridWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = FourColumnIconCardGridWidget.NAME;
    public string Heading { get; }
    public string ShortDescription { get; }
    public IEnumerable<Card> FeaturesCards { get; }
    public string FooterText { get; }

    public FourColumnIconCardGridWidgetViewModel(FourColumnIconCardGridWidgetProperties props, IEnumerable<Card> cards)
    {
        Heading = props.Heading;
        ShortDescription = props.ShortDescription;
        FeaturesCards = cards;
        FooterText = props.FooterText;
    }
}