# CTA Button Widget

## Overview
The CTA (Call-To-Action) Button Widget is a configurable button component designed to highlight clickable actions on your pages. It provides flexible alignment, margin control, and link configuration for creating compelling calls-to-action.

**Widget Identifier:** `Website.CtaButtonWidget`  
**Icon:** Rectangle  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Text** | String | Link Text | The text displayed on the button | "" (empty) |
| **LinkURL** | String | Link URL | The URL where the button links to | "" (empty) |
| **HorizontalAlignment** | Enum | Alignment | The horizontal alignment of the CTA button (Left, Center) | `Left` |
| **TopMargin** | Enum | Top Margin | The top margin spacing of the CTA (None, Small, Medium, Large) | `Medium` |
| **BottomMargin** | Enum | Bottom Margin | The bottom margin spacing of the CTA (None, Small, Medium, Large) | `Medium` |

---

## Configuration Options

### Horizontal Alignments
- **Left** - Aligns the button to the left
- **Center** - Centers the button horizontally

### Margin Sizes
- **None** - No margin/spacing
- **Small** - Small spacing
- **Medium** - Medium spacing (default)
- **Large** - Large spacing

---

## Usage Example

1. Add the widget to a page
2. Enter button text (e.g., "Learn More", "Contact Us")
3. Provide the link URL (internal or external)
4. Choose alignment (Left or Center)
5. Set top and bottom margins as needed

---

## Code Reference

**Component Class:** `CTAButtonWidget`  
**Properties Class:** `CTAButtonWidgetProperties`  
**View Model Class:** `CTAButtonWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/CTAButton/CTAButton.cshtml`

---

## Notes

- Both text and URL are required fields for functionality
- Margin settings control spacing above and below the button
- The alignment property determines button placement within its container
- Compatible with all page layouts and sections

---

## Related Widgets

- [Heading Widget](./Heading.md) - Use alongside for section headers
- [Embed Widget](./Embed.md) - Combine for rich content with CTAs

---

**Last Updated:** March 2026
