# Embed Widget

## Overview
The Embed Widget allows you to embed external HTML content (videos, iframes, maps, etc.) on your pages. It retrieves embed code from an Embed Content item and displays it with flexible sizing and alignment options.

**Widget Identifier:** `Website.EmbedWidget`  
**Icon:** XML Tag  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **SelectedEmbeds** | ContentItemReference | Selected embed | The Embed content item to display (1 item required) | [] (empty) |
| **ComponentWidth** | Enum | Width | The width of the embed (Natural, Full) | `Full` |
| **HorizontalAlignment** | Enum | Alignment | Horizontal alignment when width is Natural (Left, Center) | `Left` |

---

## Configuration Options

### Component Widths
- **Full** - Embed takes up full container width
- **Natural** - Embed displays at its natural dimensions (allows alignment configuration)

### Horizontal Alignments
- **Left** - Aligns to the left
- **Center** - Centers horizontally
- *Note: Only visible when ComponentWidth is set to "Natural"*

---

## Content Requirements

The selected Embed content item must include:
- Valid HTML embed code in the `EmbedContentHTML` field
- This could be:
  - YouTube/Vimeo player embed codes
  - Google Maps iframes
  - Custom HTML embeds
  - Third-party widget codes

---

## Error Handling

The widget displays an error message if:
- No embed has been selected
- The selected content item no longer exists
- The selected embed has no embed code

---

## Usage Example

1. Create an Embed Content item with embed code (e.g., YouTube iframe)
2. Add the Embed Widget to a page
3. Select the created Embed content item
4. Choose between Full width or Natural width
5. If Natural width, select alignment preference

---

## Code Reference

**Component Class:** `EmbedWidget`  
**Properties Class:** `EmbedWidgetProperties`  
**View Model Class:** `EmbedWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/Embed/Embed.cshtml`

---

## Notes

- Exactly 1 embed must be selected (minimum and maximum items = 1)
- The widget performs content validation and shows user-friendly error messages
- HTML embed codes are rendered as raw HTML
- Suitable for responsive iframes and embeds
- Caching is enabled for performance

---

## Related Widgets

- [Video Widget](./Video.md) - For media file embeds
- [Image Widget](./Image.md) - For static image content

---

**Last Updated:** March 2026
