# Card List Widget

## Overview
The Card List Widget displays a responsive grid of Card content items. It supports configurable columns, optional section headings, and toggleable display of images, descriptions, and CTA buttons.

**Widget Identifier:** `Website.CardListWidget`  
**Icon:** L Header List Image  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **SelectedCards** | ContentItemReference[] | Selected cards | One or more Card content items to display (minimum 1) | [] (empty) |
| **Heading** | String | Section heading | Optional heading displayed above the card grid | "" (empty) |
| **Columns** | Enum | Columns | Number of columns in the card grid (One, Two, Three, Four) | `Three` |
| **ShowImage** | Boolean | Show card image? | Display the card image at the top of each card | `true` |
| **ShowDescription** | Boolean | Show card description? | Display the short description on each card | `true` |
| **ShowCta** | Boolean | Show CTA button? | Display a CTA button linking to the card's target URL | `true` |
| **TopMargin** | Enum | Top Margin | Spacing above the widget (None, Small, Medium, Large) | `None` |
| **BottomMargin** | Enum | Bottom Margin | Spacing below the widget (None, Small, Medium, Large) | `None` |

---

## Form Organisation

Properties are organised in categories:
- **Content** — Selected cards and section heading (Order 1–3)
- **Display** — Columns, visibility toggles, and margin options (Order 4–9)

---

## Configuration Options

### Columns
| Value | Description |
|-------|-------------|
| `One` | Full-width single column |
| `Two` | Two equal columns |
| `Three` | Three equal columns (default) |
| `Four` | Four equal columns |

### Margin Sizes
- **None** — No spacing (default)
- **Small** — Small spacing
- **Medium** — Medium spacing
- **Large** — Large spacing

---

## Content Requirements

### Card Content Type (`Website.Card`)
Each Card content item should include:
- **BasicItemTitle** — Card heading
- **BasicItemShortDescription** — Card body text
- **CardImage** — Linked `ImageContent` item (optional, used when `ShowImage = true`)
- **CardLinkTitle** — CTA button label (optional)
- **CardLinkUrl** — Linked web page for CTA (optional `WebPageRelatedItem`)

---

## Error Handling

The widget displays an error message if:
- No cards have been selected
- All selected cards no longer exist in the content hub

---

## Usage Example

1. Create one or more **Card** content items in the Content Hub
2. Add the **Card List Widget** to a page
3. Select the desired Card items from the content selector
4. Set an optional section heading (e.g. "Our Services")
5. Choose the number of columns (1–4)
6. Toggle image, description, and CTA visibility as needed
7. Set top/bottom margin spacing

---

## Code Reference

**Component Class:** `CardListWidget`  
**Properties Class:** `CardListWidgetProperties`  
**View Model Class:** `CardListWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/CardList/CardList.cshtml`

**Content Type:** `Card` (`Website.Card`) — reusable content item with image, title, description, and CTA link

---

## Features

- **Responsive Grid** — Bootstrap-based responsive column layout
- **Content Ordering** — Cards displayed in editor-defined selection order
- **Flexible Visibility** — Individual toggles for image, description, CTA
- **URL Resolution** — CTA URLs resolved from web page references at render time
- **Cache Enabled** — Output cached for performance

---

## Notes

- At least 1 Card must be selected for the widget to render
- Card items can be created directly from within the content selector
- CTA URL is resolved from the linked web page — the linked page must be published
- Card order follows the selection order set by the editor

---

## Best Practices

1. Keep card titles short and scannable (3–7 words)
2. Use consistent image dimensions across cards in the same grid
3. Use 3 or 4 columns for feature grids; 2 columns for larger content cards
4. Provide CTA text and URL together — a link without text will not render a button
5. Test responsive layout at tablet and mobile breakpoints

---

## Related Widgets

- [Hero Widget](./Hero.md) — Full-width banner above card grids
- [Heading Widget](./Heading.md) — Section headings above card grids
- [CTA Button Widget](./CTAButton.md) — Standalone call-to-action buttons

---

**Last Updated:** June 2026
