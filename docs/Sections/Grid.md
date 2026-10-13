# Grid Section

## Overview
The Grid Section provides a highly customizable multi-column layout with flexible widget zone configurations. It supports multiple grid layouts, content alignment options, spacing control, and background styling for creating diverse page layouts.

**Section Identifier:** `Website.Components.Sections.Grid.Grid-Section`  
**Icon:** Header List Image  
**Custom Properties:** Yes  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Heading** | String | Heading | Optional section heading | "" (empty) |
| **HeadingAlignment** | Enum | Heading Alignment | Alignment of heading text (Left, Center) | `Left` |
| **BodyAlignment** | Enum | Body Alignment | Alignment of body content (Left, Center) | `Left` |
| **IsHeadingAnchorLinkVisible** | Boolean | Is Heading Anchor Link Visible? | Display anchor link adjacent to heading | `false` |
| **Layout** | Enum | Layout | Grid column layout configuration | `_1_Column` |
| **PaddingTop** | Enum | Padding Top | Top padding/spacing (None, Small, Medium, Large) | `Large` |
| **PaddingBottom** | Enum | Padding Bottom | Bottom padding/spacing (None, Small, Medium, Large) | `Large` |
| **WidgetZonesCount** | Integer | Number of Widget Zones | Number of columns/zones (1-6 range) | `1` |
| **BackgroundColor** | Enum | Background Color | Section background styling | `White` |

---

## Configuration Options

### Layout Options
- **_1_Column_Narrow** - Single column with narrow width
- **_1_Column_Max_Width_Limited** - Single column with max-width constraint
- **_1_Column** - Single column standard width (default)
- **_1_Column_Full_Width** - Single column full width
- **_2_Columns_50_50** - Two equal columns
- **_3_Columns_33_33_33** - Three equal columns

### Alignment Options
- **Left** - Left alignment (default)
- **Center** - Center alignment

### Padding/Spacing
- **None** - No padding
- **Small** - Small padding
- **Medium** - Medium padding
- **Large** - Large padding (default)

### Background Colors
- **Light** - Light background
- **White** - White background (default)
- **Dark** - Dark background
- **Secondary_Light** - Secondary light color

---

## Features

### Dynamic Widget Zones
- Configure 1-6 widget zones per section
- Each zone can hold multiple widgets
- Zones are numbered and organized by layout

### Heading Support
- Optional section heading
- Heading and body content can have different alignments
- Optional anchor links for deep linking
- Only visible when heading is populated

### Flexible Spacing
- Independent top and bottom padding control
- Responsive spacing options
- Maintains consistent vertical rhythm

---

## Usage Example

### Basic Two-Column Layout
1. Add Grid Section to page
2. Set Layout to "2_Columns_50_50"
3. Set WidgetZonesCount to 2
4. Configure padding and background
5. Add widgets to each column zone

### Hero Section with Grid
1. Add Grid Section
2. Set Heading to section title
3. Configure HeadingAlignment and BodyAlignment
4. Set background color (Dark for contrast)
5. Add content to widget zones

---

## Code Reference

**Component Class:** `GridSection`  
**Properties Class:** `GridSectionProperties`  
**View Model Class:** `GridSectionViewModel`  
**View Path:** `~/Components/PageBuilder/Sections/Grid/Grid.cshtml`

---

## Widget Zone Naming Convention

Widget zones are typically named:
- Zone-1, Zone-2, Zone-3, etc.
- Or column-specific names depending on implementation

Refer to the Grid.cshtml view for specific zone naming.

---

## Notes

- WidgetZonesCount accepts values 1-6 (validated via Range attribute)
- HeadingAlignment is only visible when Heading is populated (VisibleIfNotEmpty)
- All background colors have predefined CSS classes for styling
- Layout directly impacts the number and arrangement of widget zones
- Padding is applied to entire section, not individual zones

---

## Best Practices

1. **Match zones to layout** - Set WidgetZonesCount equal to columns in Layout
2. **Use consistent padding** - Maintain vertical rhythm with padding choices
3. **Leverage background colors** - Use Dark for contrast with light text
4. **Headings for organization** - Use section headings for content organization
5. **Test responsiveness** - Verify multi-column layouts on mobile devices

---

## Responsive Behavior

- Layouts adapt to screen sizes
- Multi-column layouts typically stack on mobile
- Padding maintains consistency across devices
- Alignment applies to all screen sizes

---

## Related Sections

- [Single Column Section](./SingleColumn.md) - Simpler single-column alternative
- [Two Column Section](./TwoColumn.md) - Fixed two-column layout
- [Heading Section](./Heading.md) - Dedicated header section

---

**Last Updated:** March 2026
