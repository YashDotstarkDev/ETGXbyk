# Heading Section

## Overview
The Heading Section is a specialized section designed for prominent page headers and hero sections. It provides a dedicated area for displaying page-level headings with minimal configuration, ideal for creating visual impact at the top of pages.

**Section Identifier:** `Website.Sections.Heading`  
**Icon:** I Header  
**Custom Properties:** None (uses default ISectionProperties)  

---

## Features

### Dedicated Header Space
- Purpose-built for page headings
- Minimal styling options (no properties)
- Consistent visual presentation

### Simple Implementation
- No configuration properties required
- Straightforward setup
- Follows convention-based styling

---

## Usage Example

### Page Hero/Header
1. Add Heading Section at top of page
2. Add Heading Widget to the section
3. Configure heading text and level
4. Style with CSS classes (section--hero, etc.)

### Documentation Page Header
1. Use Heading Section for page title
2. Add main page heading
3. Section maintains consistent header treatment
4. Rest of page uses other section types

### Blog Post Header
1. Place Heading Section above content
2. Add blog post title as heading
3. Optional subtitle or description
4. Maintains visual separation from body content

---

## Code Reference

**Component Class:** `HeadingSection`  
**Properties Class:** `HeadingSectionProperties` (empty, extends ISectionProperties)  
**View Path:** `~/Components/PageBuilder/Sections/Heading/HeadingSection.cshtml`

---

## View Implementation

The Heading Section uses a custom view name for rendering:
```
~/Components/PageBuilder/Sections/Heading/HeadingSection.cshtml
```

---

## Widget Zones

The specific widget zones available depend on the HeadingSection.cshtml implementation. Refer to the view file for zone names and layout.

---

## HTML Structure

Based on standard section patterns, the HTML typically includes:
- Section wrapper with styling classes
- Container for responsive width
- Widget zone(s) for content

---

## CSS Classes

Standard section CSS classes apply:
- `section` - Section wrapper
- `section--heading` or similar - Heading-specific styling
- `container` - Content container
- Custom classes from HeadingSection.cshtml

---

## Styling Approach

The Heading Section relies on:
- Predefined CSS classes
- View-level styling
- Standard section convention styling
- Custom classes defined in the view

---

## Notes

- No configuration properties to manage
- Simplified for specific use case (headers)
- Works best with Heading Widget
- Minimal customization through properties
- Styling primarily CSS-driven

---

## Best Practices

1. **Use for page-level headers** only
2. **Combine with Heading Widget** for content
3. **Place at top of pages** for visual hierarchy
4. **Use semantic heading levels** (H1 for main title)
5. **Maintain consistent styling** across pages
6. **Test visual presentation** on all devices

---

## Accessibility Considerations

- Proper heading hierarchy improves semantics
- Screen readers can navigate section structure
- Sufficient color contrast in styling
- Semantic HTML elements properly marked

---

## Common Patterns

### Hero Section
```
Heading Section
  ?? Heading Widget (H1)
  ?? Additional content if needed
```

### Article Header
```
Heading Section
  ?? Heading Widget (H1 - Article Title)
  ?? Optional: metadata, dates, authors
```

### Landing Page Header
```
Heading Section
  ?? Heading Widget (H1 - Main Title)
  ?? Optional: tagline or subtitle
```

---

## Comparison with Other Sections

| Section | Purpose | Configuration | Zones |
|---------|---------|----------------|-------|
| **Heading** | Page headers | None | Custom |
| **Single Column** | Body content | Flexible | 1 |
| **Two Column** | Sidebar layouts | None | 2 |
| **Grid** | Complex layouts | Highly flexible | 1-6 |
| **Unstyled** | Custom styling | Minimal | 1 |

---

## Related Sections

- [Single Column Section](./SingleColumn.md) - Body content
- [Grid Section](./Grid.md) - Multi-section layouts
- [Two Column Section](./TwoColumn.md) - Sidebar layouts

---

## Related Widgets

- [Heading Widget](../Widgets/Heading.md) - Primary content for this section
- [CTA Button Widget](../Widgets/CTAButton.md) - Common complement to headers

---

**Last Updated:** March 2026
