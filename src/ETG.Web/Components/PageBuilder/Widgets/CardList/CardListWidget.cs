using System.ComponentModel;
using CMS.ContentEngine;
using CMS.Websites;
using CSharpFunctionalExtensions;
using ETG.Core.Components;
using ETG.Core.Content;
using ETG.Core.Operations;
using ETG.Web.Components.PageBuilder.Widgets.CardList;
using ETG.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: CardListWidget.IDENTIFIER,
    viewComponentType: typeof(CardListWidget),
    name: CardListWidget.NAME,
    propertiesType: typeof(CardListWidgetProperties),
    Description = "Displays a responsive grid of Card content items.",
    IconClass = KenticoIcons.L_HEADER_LIST_IMG,
    AllowCache = true)]

namespace ETG.Web.Components.PageBuilder.Widgets.CardList;

public class CardListWidget(IContentRetriever contentRetriever, ContentItemQueryTools tools, IWebPageUrlRetriever urlRetriever) : ViewComponent
{
    public const string IDENTIFIER = "Website.CardListWidget";
    public const string NAME = "Card List";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly ContentItemQueryTools tools = tools;
    private readonly IWebPageUrlRetriever urlRetriever = urlRetriever;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CardListWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        var cardGuids = (props.SelectedCards ?? []).Select(i => i.Identifier).ToArray();

        if (cardGuids.Length == 0)
        {
            return View(
                "~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "No cards have been selected."));
        }

        var cards = (await contentRetriever
            .RetrieveContentByGuids<Card>(
                cardGuids,
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 }))
            .OrderBy(c => Array.IndexOf(cardGuids, c.SystemFields.ContentItemGUID))
            .ToList();

        if (cards.Count == 0)
        {
            return View(
                "~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected cards no longer exist."));
        }

        // Resolve CTA URLs from WebPageRelatedItem references
        var cardItems = new List<CardListItem>();

        foreach (var card in cards)
        {
            string ctaUrl = "#";
            var linkItem = card.CardLinkUrl?.FirstOrDefault();

            if (linkItem?.WebPageGuid != null && linkItem.WebPageGuid != Guid.Empty)
            {
                var urls = await urlRetriever.Retrieve(
                    [linkItem.WebPageGuid],
                    tools.WebsiteChannelContext.WebsiteChannelName,
                    "en",
                    tools.WebsiteChannelContext.IsPreview,
                    CancellationToken.None);

                if (urls != null && urls.TryGetValue(linkItem.WebPageGuid, out var pageUrl))
                {
                    ctaUrl = pageUrl.RelativePath ?? "#";
                }
            }

            var imageContent = card.CardImage?.FirstOrDefault();

            cardItems.Add(new CardListItem
            {
                Title = card.BasicItemTitle ?? "",
                Description = card.BasicItemShortDescription ?? "",
                Image = imageContent?.LegacyMediaFileAsset != null
                    ? ImageAssetViewModel.Create(imageContent)
                    : null,
                LinkTitle = card.CardLinkTitle ?? "",
                LinkUrl = ctaUrl,
                HasLink = linkItem != null || !string.IsNullOrWhiteSpace(card.CardLinkTitle)
            });
        }

        return View(
            "~/Components/PageBuilder/Widgets/CardList/CardList.cshtml",
            new CardListWidgetViewModel(props, cardItems));
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class CardListWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        Card.CONTENT_TYPE_NAME,
        Label = "Selected cards",
        ExplanationText = "Select one or more Card content items to display",
        MinimumItems = 1,
        AllowContentItemCreation = true,
        Order = 2)]
    public IEnumerable<ContentItemReference> SelectedCards { get; set; } = [];

    [TextInputComponent(
        Label = "Section heading",
        ExplanationText = "Optional heading displayed above the card grid",
        WatermarkText = "Our featured cards",
        Order = 3)]
    public string Heading { get; set; } = "";

    [DropDownComponent(
        Label = "Columns",
        ExplanationText = "Number of columns in the card grid",
        Tooltip = "Select the number of columns",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CardListColumns>),
        Order = 4)]
    public string Columns { get; set; } = nameof(CardListColumns.Three);
    public CardListColumns ColumnsParsed => EnumDropDownOptionsProvider<CardListColumns>.Parse(Columns, CardListColumns.Three);

    [CheckBoxComponent(
        Label = "Show card image?",
        ExplanationText = "If enabled, the card image will be displayed at the top of each card.",
        Order = 5)]
    public bool ShowImage { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show card description?",
        ExplanationText = "If enabled, the short description will be displayed on each card.",
        Order = 6)]
    public bool ShowDescription { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show CTA button?",
        ExplanationText = "If enabled, a button linking to the card's target URL is shown.",
        Order = 7)]
    public bool ShowCta { get; set; } = true;

    [DropDownComponent(
        Label = "Top Margin",
        ExplanationText = "Top margin above the widget",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CardListMarginSizes>),
        Order = 8)]
    public string TopMargin { get; set; } = nameof(CardListMarginSizes.None);
    public CardListMarginSizes TopMarginParsed => EnumDropDownOptionsProvider<CardListMarginSizes>.Parse(TopMargin, CardListMarginSizes.None);

    [DropDownComponent(
        Label = "Bottom Margin",
        ExplanationText = "Bottom margin below the widget",
        DataProviderType = typeof(EnumDropDownOptionsProvider<CardListMarginSizes>),
        Order = 9)]
    public string BottomMargin { get; set; } = nameof(CardListMarginSizes.None);
    public CardListMarginSizes BottomMarginParsed => EnumDropDownOptionsProvider<CardListMarginSizes>.Parse(BottomMargin, CardListMarginSizes.None);
}

public enum CardListColumns
{
    [Description("1 Column")]
    One,
    [Description("2 Columns")]
    Two,
    [Description("3 Columns")]
    Three,
    [Description("4 Columns")]
    Four
}

public enum CardListMarginSizes
{
    [Description("None")]
    None,
    [Description("Small")]
    Small,
    [Description("Medium")]
    Medium,
    [Description("Large")]
    Large
}

public class CardListItem
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public ImageAssetViewModel? Image { get; set; }
    public string LinkTitle { get; set; } = "";
    public string LinkUrl { get; set; } = "#";
    public bool HasLink { get; set; }
}

public class CardListWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = CardListWidget.NAME;

    public string Heading { get; }
    public bool HasHeading { get; }
    public IReadOnlyList<CardListItem> Cards { get; }
    public CardListColumns Columns { get; }
    public bool ShowImage { get; }
    public bool ShowDescription { get; }
    public bool ShowCta { get; }
    public CardListMarginSizes TopMargin { get; }
    public CardListMarginSizes BottomMargin { get; }

    public CardListWidgetViewModel(CardListWidgetProperties props, IReadOnlyList<CardListItem> cards)
    {
        Heading = props.Heading;
        HasHeading = !string.IsNullOrWhiteSpace(props.Heading);
        Cards = cards;
        Columns = props.ColumnsParsed;
        ShowImage = props.ShowImage;
        ShowDescription = props.ShowDescription;
        ShowCta = props.ShowCta;
        TopMargin = props.TopMarginParsed;
        BottomMargin = props.BottomMarginParsed;
    }
}
