# Single Column Section

## Overview
The Single Column Section provides a straightforward single-column layout with flexible padding, alignment, and background styling options. It's ideal for simple page structures, article content, and full-width content blocks.

**Section Identifier:** `Website.Sections.SingleColumn`  
**Icon:** Square  
**Custom Properties:** Yes  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Layout** | Enum | Layout | Column width configuration (Standard, Narrow, Full_Width) | `Standard` |
| **Alignment** | Enum | Alignment | Horizontal alignment of content (Left, Center) | `Left` |
| **PaddingTop** | Enum | Padding Top | Top padding/spacing (None, Small, Medium, Large) | `Large` |
| **PaddingBottom** | Enum | Padding Bottom | Bottom padding/spacing (None, Small, Medium, Large) | `Large` |
| **BackgroundColor** | Enum | Background Color | Section background styling | `White` |

---

## Configuration Options

### Layout Options
- **Standard** - Standard column width (default)
- **Narrow** - Narrower column width
- **Full_Width** - Full container width

### Alignment Options
- **Left** - Left alignment (default)
- **Center** - Center alignment
- *Note: Individual widgets can override this setting*

### Padding/Spacing
- **None** - No padding
- **Small** - Small padding
- **Medium** - Medium padding
- **Large** - Large padding (default)

### Background Colors
- **White** - White background (default)
- **Light** - Light background
- **Secondary_Light** - Secondary light color
- **Dark** - Dark background

---

## Single Widget Zone

The Single Column Section contains one main widget zone that spans the full column width. This zone can hold multiple widgets stacked vertically.

---

## Features

### Flexible Width Control
- Adjust column width with Layout option
- Standard width for most content
- Narrow for focused content
- Full width for maximum space usage

### Content Alignment
- Section-level alignment setting
- Individual widgets can override alignment
- Allows both left and center positioning

### Vertical Spacing
- Independent top and bottom padding control
- Maintains consistent vertical rhythm
- Responsive padding adjustments

### Background Styling
- Multiple background color options
- Predefined color scheme
- Suitable for creating visual sections

---

## Usage Example

### Article/Blog Post Section
1. Add Single Column Section
2. Set Layout to "Standard"
3. Set Alignment to "Left"
4. Configure padding (Large top and bottom)
5. Set background to "White"
6. Add text, images, and other content to zone

### Narrow Focus Section
1. Add Single Column Section
2. Set Layout to "Narrow"
3. Set Alignment to "Center"
4. Use Dark background for contrast
5. Add featured content (e.g., testimonials, quotes)

### Full-Width Banner
1. Add Single Column Section
2. Set Layout to "Full_Width"
3. Set Alignment to "Center"
4. Set background color appropriately
5. Add banner content (heading, CTA button, etc.)

---

## Code Reference

**Component Class:** `SingleColumnSection`  
**Properties Class:** `SingleColumnSectionProperties`  
**View Model Class:** `SingleColumnSectionViewModel`  
**View Path:** `~/Components/PageBuilder/Sections/SingleColumn/SingleColumn.cshtml`

---

## Widget Zone

**Zone Name:** Typically "main" or "content"  
Refer to SingleColumn.cshtml view for specific zone identifier.

---

## Notes

- Single zone simplifies layout but allows multiple widgets
- Widgets stack vertically within the zone
- Alignment can be overridden per-widget
- Background applies to entire section
- Padding maintains consistent spacing between sections

---

## Best Practices

1. **Use Standard layout** for most content pages
2. **Use Narrow layout** for focused content like testimonials
3. **Use Full_Width layout** for banners and special sections
4. **Set padding consistently** - use Large for section separation
5. **Choose background colors** that provide visual hierarchy
6. **Test content flow** on different screen sizes

---

## Responsive Behavior

- Column width adjusts on smaller screens
- Padding may scale on mobile devices
- Single zone ensures clean stacking on all sizes
- Alignment maintained across screen sizes

---

## Comparison with Other Sections

| Section | Columns | Use Case |
|---------|---------|----------|
| **Single Column** | 1 | Simple, linear content |
| **Two Column** | 2 (fixed) | Sidebar layouts |
| **Grid** | 1-6 (flexible) | Complex layouts |
| **Heading** | Dedicated | Page headers |
| **Unstyled** | 1 | Custom styling |

---

## Related Sections

- [Grid Section](./Grid.md) - Multi-column layouts
- [Two Column Section](./TwoColumn.md) - Two-column layouts
- [Heading Section](./Heading.md) - Page headers

---

**Last Updated:** March 2026
