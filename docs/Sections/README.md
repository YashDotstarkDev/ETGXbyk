# Page Builder Sections

## Overview
This section contains comprehensive documentation for all available Page Builder Sections in the DotStark Xperience by Kentico Accelerator. Sections are layout containers that hold widget zones and define the structural layout of your pages.

---

## Available Sections

| # | Section Name | Identifier | Widget Zones | Purpose | Icon |
|---|--------------|-----------|--------------|---------|------|
| 1 | [Grid](./Grid.md) | `Website.Components.Sections.Grid.Grid-Section` | 1-6 (customizable) | Flexible multi-column layouts | Header List Image |
| 2 | [Single Column](./SingleColumn.md) | `Website.Sections.SingleColumn` | 1 | Simple single-column content | Square |
| 3 | [Two Column](./TwoColumn.md) | `Website.Sections.TwoColumn` | 2 (fixed) | Sidebar + main content layout | - |
| 4 | [Heading](./Heading.md) | `Website.Sections.Heading` | 1+ | Dedicated page headers | I Header |
| 5 | [Unstyled](./Unstyled.md) | `Website.Sections.Unstyled` | 1 | Custom styling container | Rectangle Outline |

---

## Section Categories

### Single Column Layouts
Simple, linear layouts for straightforward content:
- **[Single Column](./SingleColumn.md)** - Standard single-column layout
- **[Heading](./Heading.md)** - Dedicated header sections

### Multi-Column Layouts
Layouts with multiple columns for complex arrangements:
- **[Two Column](./TwoColumn.md)** - Fixed two-column with sidebar
- **[Grid](./Grid.md)** - Flexible 1-6 column layouts

### Custom Layouts
For unique designs and requirements:
- **[Unstyled](./Unstyled.md)** - Blank canvas for custom CSS

---

## Comparison Chart

### By Configuration Flexibility

| Section | Properties | Customization | Best For |
|---------|------------|---------------|----------|
| **Unstyled** | None | Maximum (CSS-driven) | Custom designs |
| **Grid** | Multiple | High (admin UI) | Complex layouts |
| **Single Column** | 5 | Medium (admin UI) | Standard content |
| **Two Column** | None | Low (fixed structure) | Sidebar layouts |
| **Heading** | None | Minimal | Page headers |

### By Number of Widget Zones

| Section | Zones | Type | Flexibility |
|---------|-------|------|-------------|
| **Grid** | 1-6 | Dynamic | Highly flexible |
| **Two Column** | 2 | Fixed | Fixed |
| **Single Column** | 1 | Fixed | Limited |
| **Heading** | 1+ | Variable | As defined in view |
| **Unstyled** | 1 | Fixed | Custom CSS |

---

## Common Properties

Most sections (except Two Column, Heading, and Unstyled) share similar properties:

### Layout & Structure
- **Layout** - Column configuration
- **WidgetZonesCount** - Number of zones (Grid only)

### Spacing & Padding
- **PaddingTop** - Top padding (None, Small, Medium, Large)
- **PaddingBottom** - Bottom padding (None, Small, Medium, Large)

### Alignment & Styling
- **Alignment** - Content alignment (Left, Center)
- **BackgroundColor** - Section background (White, Light, Dark, Secondary_Light)

### Headers (where applicable)
- **Heading** - Optional section title
- **HeadingAlignment** - Title alignment
- **IsHeadingAnchorLinkVisible** - Display anchor link

---

## Section Usage Guide

### Basic Layout Structure
```
Page
??? Heading Section (optional header)
??? Single Column Section (content)
??? Grid Section (featured content)
??? Two Column Section (sidebar + content)
??? Single Column Section (more content)
```

### Common Page Patterns

#### Blog Post Layout
```
1. Heading Section - Article title
2. Single Column Section - Article body
3. Single Column Section - Related posts
```

#### Landing Page
```
1. Heading Section - Hero section
2. Grid Section (3 columns) - Features
3. Single Column Section - CTA section
4. Two Column Section - FAQ + sidebar
```

#### Documentation Site
```
1. Heading Section - Page title
2. Two Column Section - Nav sidebar + content
3. Single Column Section - Additional info
```

---

## Properties Reference

### Padding Options
- **None** - No padding
- **Small** - Small spacing (~1rem)
- **Medium** - Medium spacing (~2rem)
- **Large** - Large spacing (~3rem)

### Alignment Options
- **Left** - Left alignment
- **Center** - Center alignment
- **Right** - Right alignment (where available)

### Background Colors
- **White** - White background (default)
- **Light** - Light background
- **Dark** - Dark background
- **Secondary_Light** - Secondary light color

### Layout Options (Grid/Single Column)
- **Standard** - Standard width
- **Narrow** - Narrower width
- **Full_Width** - Full container width
- **Max_Width_Limited** - Limited maximum width

---

## Creating Custom Sections

### Basic Custom Section
```csharp
[assembly: RegisterSection(
    identifier: "Your.CustomSection",
    viewComponentType: typeof(YourCustomSection),
    name: "Custom Section",
    propertiesType: typeof(YourCustomSectionProperties),
    Description = "Description")]

public class YourCustomSection : ViewComponent
{
    public IViewComponentResult Invoke(ComponentViewModel<YourCustomSectionProperties> vm)
    {
        var model = new YourCustomSectionViewModel(vm.Properties);
        return View("~/path/to/view.cshtml", model);
    }
}

public class YourCustomSectionProperties : ISectionProperties
{
    [TextInputComponent(Label = "Example Property")]
    public string ExampleProperty { get; set; }
}
```

### Advanced: Custom View Name
```csharp
[assembly: RegisterSection(
    identifier: "Your.CustomSection",
    name: "Custom Section",
    propertiesType: null,
    customViewName: "~/path/to/view.cshtml",
    Description = "Description")]
```

---

## Best Practices

### General
1. **Use appropriate sections** for content type
2. **Maintain consistent spacing** with padding options
3. **Test responsiveness** on all device sizes
4. **Keep sections focused** - one main idea per section
5. **Use white space** effectively with padding

### Layout Planning
1. **Plan page structure** before adding sections
2. **Use Heading sections** for visual separation
3. **Group related content** in same section
4. **Alternate layouts** to maintain visual interest
5. **Use Grid section** for complex arrangements

### Performance
1. **Minimize widget count** per section
2. **Use caching** where available
3. **Optimize media** in sections
4. **Test page load time** with multiple sections
5. **Profile with browser tools** for bottlenecks

### Accessibility
1. **Maintain heading hierarchy** across sections
2. **Use semantic HTML** elements
3. **Ensure color contrast** in backgrounds
4. **Support keyboard navigation** throughout
5. **Provide alt text** for images in sections

---

## Responsive Design

### Mobile Considerations
- Multi-column sections typically stack vertically
- Padding and margins scale on mobile
- Ensure readable font sizes at all breakpoints
- Test navigation and interactions on mobile
- Use mobile-first design approach

### Breakpoints
Typical breakpoints (CSS-dependent):
- **Mobile** - < 768px
- **Tablet** - 768px - 1024px
- **Desktop** - > 1024px

---

## Troubleshooting

### Section Not Rendering
- Check section identifier is correct
- Verify properties are populated
- Check console for JavaScript errors
- Confirm sections are registered

### Layout Issues
- Verify widget zones have names (check view)
- Check CSS for display issues
- Inspect with browser dev tools
- Test different layouts

### Widget Zone Problems
- Confirm zone-name matches in view
- Verify widgets are compatible with section
- Check for naming conflicts
- Review browser console for errors

---

## Section Widget Zones

### What Are Widget Zones?
- Named containers within sections
- Accept multiple widgets
- Control widget placement
- Named in section view files

### Zone Naming Convention
- Zones are named in section view files
- Common names: "main", "content", "side-panel"
- Check section view for specific names
- Multiple zones enable complex layouts

### Adding Widgets to Zones
1. Edit page in admin
2. Find the section
3. Click "+ Add Widget"
4. Select zone (if multiple available)
5. Choose widget type
6. Configure widget properties

---

## Advanced Topics

### Custom Styling
- Override CSS classes in your stylesheet
- Use BEM naming convention
- Create theme variants
- Support dark mode

### Responsive Images
- Use picture element
- Serve multiple image sizes
- Optimize for different devices
- Consider lazy loading

### Performance Optimization
- Minimize CSS/JavaScript
- Use CSS Grid for complex layouts
- Implement lazy loading
- Monitor Lighthouse scores

---

## Related Documentation

- **[Page Builder Widgets](../Widgets/README.md)** - Learn about individual widgets
- **[FEATURES.md](../FEATURES.md)** - Complete features overview
- **[Xperience Documentation](https://devnet.kentico.com)** - Official Kentico docs

---

## Quick Reference

### Choose Your Section

**Need flexible columns?** ? [Grid Section](./Grid.md)

**Simple content layout?** ? [Single Column](./SingleColumn.md)

**Sidebar + content?** ? [Two Column](./TwoColumn.md)

**Page header?** ? [Heading](./Heading.md)

**Custom design?** ? [Unstyled](./Unstyled.md)

---

## Summary Table

| Section | Zones | Properties | Customization | Caching |
|---------|-------|-----------|---------------|---------|
| Grid | 1-6 | 9 | High | Yes |
| Single Column | 1 | 5 | Medium | Yes |
| Two Column | 2 | 0 | Low | Yes |
| Heading | 1+ | 0 | Minimal | Yes |
| Unstyled | 1 | 0 | Maximum | No data |

---

**Last Updated:** March 2026  
**Total Sections:** 5  
**All Sections Documented:** ?

---

## Navigation

- [? Back to Docs](../README.md)
- [Widgets Documentation ?](../Widgets/README.md)
- [Features Overview ?](../FEATURES.md)
