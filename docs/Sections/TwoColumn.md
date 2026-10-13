# Two Column Section

## Overview
The Two Column Section provides a fixed two-column layout with a sidebar and main content area. It's ideal for content pages requiring sidebar navigation, secondary information, or supporting content alongside primary content.

**Section Identifier:** `Website.Sections.TwoColumn`  
**Custom View Name:** `~/Components/PageBuilder/Sections/TwoColumn/TwoColumn.cshtml`  
**Custom Properties:** None (uses default ISectionProperties)  

---

## Layout Structure

The Two Column Section consists of two fixed widget zones:

| Zone | Position | Purpose | Typical Width |
|------|----------|---------|----------------|
| **side-panel** | Left sidebar | Secondary content, navigation, filters | ~25-30% |
| **main** | Main content | Primary page content | ~70-75% |

---

## Widget Zones

### Side Panel Zone (Sidebar)
- **Zone Name:** `side-panel`
- **Position:** Left side
- **Purpose:** Secondary content, navigation, widgets
- **Typical Content:** Sidebar widgets, related links, filters

### Main Zone
- **Zone Name:** `main`
- **Position:** Right side
- **Purpose:** Primary page content
- **Typical Content:** Page body, articles, main content blocks

---

## Features

### Fixed Layout
- Consistent sidebar and main content split
- No configuration options (simplified)
- Suitable for standardized layouts

### Sidebar Support
- Dedicated sidebar area
- Ideal for navigation or secondary information
- Maintains visual separation from main content

### Semantic HTML
- Uses proper `<aside>` element for sidebar (accessibility)
- Uses `<main>` role for primary content

---

## Usage Example

### Documentation/Knowledge Base
1. Add Two Column Section
2. Add navigation menu or table of contents to side-panel zone
3. Add article content to main zone
4. Organize related articles in sidebar

### Blog Post with Sidebar
1. Add Two Column Section
2. Add widgets to side-panel (related posts, author info, ads)
3. Add blog post content to main zone
4. Sidebar remains fixed while main content scrolls

### Dashboard/Profile Page
1. Add Two Column Section
2. Add user menu/navigation to side-panel
3. Add profile/dashboard content to main zone
4. Create consistent navigation experience

---

## Code Reference

**Component Class:** `TwoColumnSection`  
**Properties Class:** `TwoColumnSectionProperties` (empty, extends ISectionProperties)  
**View Path:** `~/Components/PageBuilder/Sections/TwoColumn/TwoColumn.cshtml`

---

## HTML Structure

```html
<div class="section section--default">
    <div class="container">
        <div class="layout">
            <!-- Sidebar Zone -->
            <aside class="layout__sidebar">
                <widget-zone zone-name="side-panel" />
            </aside>
            
            <!-- Main Content Zone -->
            <div role="main" class="layout__main">
                <widget-zone zone-name="main" />
            </div>
        </div>
    </div>
</div>
```

---

## CSS Classes

| Class | Purpose |
|-------|---------|
| `section` | Section wrapper |
| `section--default` | Default section styling |
| `container` | Content container |
| `layout` | Layout wrapper |
| `layout__sidebar` | Sidebar styling |
| `layout__main` | Main content styling |

---

## Responsive Behavior

- On mobile devices, typically stacks vertically
- Sidebar may appear above or below main content
- Breakpoints controlled by CSS media queries
- Flexible widths adjust to screen size

---

## Notes

- No configuration properties required
- Simplified setup for consistent layouts
- Fixed two-column structure (no customization)
- CSS and styling control layout behavior
- Both zones accept multiple widgets

---

## Best Practices

1. **Use semantically appropriate content** in each zone
2. **Keep sidebar focused** - don't overcrowd with widgets
3. **Maintain visual hierarchy** between sidebar and main
4. **Test on mobile** - ensure stacking works well
5. **Balance content load** between zones
6. **Use complementary content** in sidebar (related, secondary)

---

## Accessibility Considerations

- `<aside>` element properly identifies sidebar content
- `role="main"` clearly marks primary content
- Semantic HTML improves screen reader experience
- Proper heading hierarchy across both zones
- Color contrast maintained in styling

---

## Comparison with Other Sections

| Section | Columns | Customization | Use Case |
|---------|---------|----------------|----------|
| **Two Column** | 2 (fixed) | None | Sidebar layouts |
| **Single Column** | 1 | Flexible | Simple content |
| **Grid** | 1-6 | Highly flexible | Complex layouts |
| **Heading** | - | Minimal | Page headers |
| **Unstyled** | 1 | Custom | Custom styling |

---

## Related Sections

- [Single Column Section](./SingleColumn.md) - Single column alternative
- [Grid Section](./Grid.md) - Flexible multi-column layouts
- [Heading Section](./Heading.md) - Page headers

---

**Last Updated:** March 2026
