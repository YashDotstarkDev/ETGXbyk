# Heading Widget

## Overview
The Heading Widget displays headings with flexible styling, alignment, and optional anchor link functionality. It supports all standard HTML heading levels (H1-H6) with automatic or custom anchor ID generation for deep linking.

**Widget Identifier:** `Website.HeadingWidget`  
**Icon:** H Header Text  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **HeadingText** | String | Text | The text content of the heading | "" (empty) |
| **HeadingLevel** | Enum | Heading level | HTML heading level (H1, H2, H3, H4, H5, H6) | `H2` |
| **HeadingAlignment** | Enum | Heading alignment | Alignment of heading text (Left, Center, Right) | `Left` |
| **ShowHeadingAnchor** | Boolean | Show heading anchor? | Display an anchor link adjacent to the heading | `true` |
| **AnchorOverride** | String | Anchor override | Custom anchor ID for the heading (if empty, auto-generated) | "" (empty) |

---

## Form Organization

Properties are organized in categories:
- **Content** - HeadingText (Order 1-2)
- **Display** - All styling and anchor options (Order 3-7)

---

## Configuration Options

### Heading Levels
- **H1** - Primary heading
- **H2** - Secondary heading (default)
- **H3** - Tertiary heading
- **H4** - Quaternary heading
- **H5** - Quinary heading
- **H6** - Senary heading

### Heading Alignments
- **Left** - Left-aligned text (default)
- **Center** - Centered text
- **Right** - Right-aligned text

### Anchor Links

#### Automatic Anchor Generation
- When `AnchorOverride` is empty
- Anchor ID is auto-generated from heading text
- Converts text to URL-friendly format
- Example: "About Our Company" ? "about-our-company"

#### Custom Anchor
- When `AnchorOverride` is populated
- Exact value used as HTML `id` attribute
- Must be unique on the page
- Only valid ID characters allowed (letters, numbers, hyphens, underscores)
- Example: "custom-about-section"

---

## Error Handling

The widget displays an error message if:
- Heading text is empty or whitespace-only

---

## Usage Example

1. Add Heading Widget to a page
2. Enter heading text (e.g., "About Our Company")
3. Select appropriate heading level (H1, H2, etc.)
4. Choose alignment (Left, Center, or Right)
5. Enable/disable anchor link visibility
6. Optionally provide custom anchor ID

---

## Code Reference

**Component Class:** `HeadingWidget`  
**Properties Class:** `HeadingWidgetProperties`  
**View Model Class:** `HeadingWidgetViewModel` (extends `BaseWidgetViewModel`)  
**View Path:** `~/Components/PageBuilder/Widgets/Heading/Heading.cshtml`

---

## Features

- **SEO-Friendly** - Proper heading hierarchy support
- **Anchor Links** - Deep linking within page
- **Accessibility** - Semantic HTML heading elements
- **Flexible Styling** - Multiple alignment options
- **Content Validation** - Ensures heading text is present

---

## Notes

- Heading text is required for the widget to render
- `ShowHeadingAnchor` defaults to true for link visibility
- Auto-generated anchors use URL-safe slug conversion
- Custom anchors bypass validation - use carefully
- Caching is enabled for performance

---

## Best Practices

1. Use H1 sparingly - typically only one per page
2. Maintain logical heading hierarchy (H1 ? H2 ? H3, etc.)
3. Keep anchor IDs descriptive and lowercase
4. Use auto-generated anchors for most cases
5. Test deep links to ensure proper anchor functionality
6. Avoid special characters in custom anchor IDs

---

## Accessibility Considerations

- Use proper heading hierarchy for screen readers
- Anchor links are keyboard accessible
- Ensure sufficient color contrast if styling headings
- Semantic HTML improves document structure

---

## Related Widgets

- [Embed Widget](./Embed.md) - Combine with headings for sections
- [CTA Button Widget](./CTAButton.md) - Pair with headings for calls-to-action

---

**Last Updated:** March 2026
