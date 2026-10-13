# Image Widget

## Overview
The Image Widget displays images from Xperience content items with responsive sizing, alignment, and visual styling options. It supports multiple image themes including shadow effects and provides flexible sizing configurations.

**Widget Identifier:** `Website.ImageWidget`  
**Icon:** Camera  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **SelectedImages** | ContentItemReference | Selected image | The Image content item to display (1 item required) | [] (empty) |
| **Alignment** | Enum | Image Alignment | Horizontal alignment of the image (Left, Center, Right) | `Center` |
| **Size** | Enum | Image Size | Display size of the image (Full_Width, Half_Width, Third_Width, Quarter_Width, Natural) | `Full_Width` |
| **Theme** | Enum | Image Theme | Visual styling (None, Shadow, Rounded_Corners, Rounded_With_Shadow) | `Shadow` |

---

## Form Organization

Properties are organized in categories:
- **Content** - Selected image (Order 1-2)
- **Display** - Alignment, size, and theme options (Order 3-5+)

---

## Configuration Options

### Image Alignments
- **Left** - Aligns image to the left
- **Center** - Centers the image (default)
- **Right** - Aligns image to the right

### Image Sizes
- **Full_Width** - Takes up 100% container width (default)
- **Half_Width** - Takes up 50% container width
- **Third_Width** - Takes up 33% container width
- **Quarter_Width** - Takes up 25% container width
- **Natural** - Displays at original image dimensions

### Image Themes
- **None** - No styling applied
- **Shadow** - Adds drop shadow effect (recommended for screenshots)
- **Rounded_Corners** - Rounded border corners
- **Rounded_With_Shadow** - Combines rounded corners and shadow

---

## Content Requirements

The selected Image content item must include:
- An asset file (image file attached)
- Optional alt text for accessibility
- Optional description

---

## Error Handling

The widget displays an error message if:
- No image has been selected
- The selected content item no longer exists
- The image file no longer exists or failed to attach

---

## Usage Example

1. Create or select an Image content item with an image asset
2. Add Image Widget to a page
3. Select the image from the content selector
4. Choose alignment (Left, Center, or Right)
5. Set size (Full_Width by default)
6. Apply visual theme (Shadow recommended)

---

## Code Reference

**Component Class:** `ImageWidget`  
**Properties Class:** `ImageWidgetProperties`  
**View Model Class:** `ImageWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/Image/Image.cshtml`

**Content Type:** `ImageContent` - Contains image asset and metadata

---

## Features

- **Responsive Images** - Adapts to container sizes
- **Multiple Themes** - Visual styling options
- **Flexible Sizing** - Multiple predefined sizes
- **Alt Text Support** - Accessibility-friendly
- **Asset Management** - Integrated with Xperience assets

---

## Notes

- Exactly 1 image must be selected (minimum and maximum items = 1)
- Content item creation is allowed directly from widget selector
- Default grid view mode in selector (recommended for visual preview)
- Shadow theme is recommended for screenshots and professional images
- Caching is enabled for performance

---

## Best Practices

1. **Always provide alt text** on image content items for accessibility
2. **Use Shadow theme** for screenshots and professional images
3. **Optimize image sizes** before uploading to reduce page load time
4. **Choose appropriate sizing** based on content context
5. **Test responsive behavior** on different screen sizes
6. **Use meaningful image descriptions** in content items

---

## Accessibility Considerations

- Alt text is required for screen readers
- Theme styles should maintain visual hierarchy
- Sufficient color contrast should be maintained
- Images should be decorative or contextual

---

## Related Widgets

- [Video Widget](./Video.md) - For video content
- [Embed Widget](./Embed.md) - For embedded media
- [Heading Widget](./Heading.md) - Combine with images for visual sections

---

**Last Updated:** March 2026
