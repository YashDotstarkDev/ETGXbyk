# Unstyled Section

## Overview
The Unstyled Section is a minimalist section component with no predefined styling or properties. It provides a blank canvas for creating custom layouts and styling, making it ideal for unique designs that don't fit standard section templates.

**Section Identifier:** `Website.Sections.Unstyled`  
**Icon:** Rectangle Outline Horizontal  
**Custom View:** `~/Components/PageBuilder/Sections/Unstyled/Unstyled.cshtml`  
**Custom Properties:** None (no properties available)  
**Description:** A completely unstyled section with a single Widget Zone. Can be used as a placeholder section.

---

## Features

### Minimal Styling
- No predefined CSS classes or styles
- Complete freedom for custom styling
- Blank canvas approach
- Placeholder functionality

### Single Widget Zone
- One widget zone for content
- Supports multiple widgets
- Flexible content arrangement

### No Configuration
- No properties to configure
- No styling options in admin
- Pure HTML structure

---

## Use Cases

### Custom Designs
When a page design doesn't align with standard sections:
- Unique layouts requiring custom CSS
- Experimental designs
- One-off page designs
- Special landing pages

### Placeholder Content
Use as temporary sections:
- Section holders during development
- Placeholder during content creation
- Testing and prototyping

### Full Custom Control
For developers needing complete control:
- Custom CSS frameworks
- Specific design requirements
- Non-standard layouts
- Specialized functionality

---

## HTML Structure

Based on the unstyled approach, the view provides minimal markup:
- Basic section wrapper
- Single widget zone
- Minimal CSS classes
- Clean, semantic HTML

---

## Widget Zone

**Zone Name:** Typically "content" or similar (check Unstyled.cshtml)

The zone accepts multiple widgets that can be styled with custom CSS.

---

## Code Reference

**Component Class:** `UnstyledSection`  
**Properties Class:** None (no properties defined)  
**View Path:** `~/Components/PageBuilder/Sections/Unstyled/Unstyled.cshtml`
**Custom View Name:** Specified in register attribute

---

## Implementation Example

When using the Unstyled Section, styling is entirely CSS-driven:

```css
/* Custom styles for unstyled section */
.unstyled-section {
    /* Your custom styling here */
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 2rem;
}

.unstyled-section > .widget {
    /* Widget-specific styling */
    padding: 1rem;
    border: 1px solid #ccc;
}
```

---

## Best Practices

1. **Plan your layout carefully** before implementing
2. **Use CSS frameworks** if needed (Bootstrap, Tailwind, etc.)
3. **Test responsiveness thoroughly**
4. **Document custom styling** for maintenance
5. **Keep markup semantic** despite minimal styling
6. **Avoid inline styles** - use external stylesheets
7. **Consider accessibility** in custom designs

---

## Notes

- No admin-level configuration available
- Styling must be applied via external CSS
- View file controls HTML structure
- Single zone keeps structure simple
- Best for developers with CSS expertise

---

## Responsive Design

Since this section has no built-in responsive behavior:
- Implement responsive CSS yourself
- Use CSS media queries
- Test on all breakpoints
- Consider mobile-first approach
- Ensure touch-friendly on mobile devices

---

## Accessibility Considerations

- Maintain semantic HTML structure
- Use proper heading hierarchy
- Ensure sufficient color contrast
- Provide alt text for images
- Make interactive elements keyboard accessible
- Use ARIA attributes when necessary

---

## Comparison with Other Sections

| Section | Styling | Customization | Use Case |
|---------|---------|---------------|----------|
| **Unstyled** | None | Complete | Custom designs |
| **Single Column** | Predefined | Limited | Standard content |
| **Two Column** | Fixed | None | Sidebar layouts |
| **Grid** | Predefined | High | Complex layouts |
| **Heading** | Predefined | Minimal | Page headers |

---

## Development Tips

### Working with Unstyled Section

1. **View the generated HTML** in browser dev tools
2. **Start with minimal CSS** and build up
3. **Use browser inspector** to debug layouts
4. **Test responsive behavior** frequently
5. **Create reusable CSS classes** for consistency

### Common Patterns

**Card Grid Layout:**
```css
.unstyled-section {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 2rem;
}
```

**Flex Row Layout:**
```css
.unstyled-section {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
}
```

**Masonry Layout:**
```css
.unstyled-section {
    column-count: 3;
    column-gap: 2rem;
}
```

---

## Related Sections

- [Single Column Section](./SingleColumn.md) - Styled alternative
- [Grid Section](./Grid.md) - Pre-configured layouts
- [Two Column Section](./TwoColumn.md) - Structured layouts
- [Heading Section](./Heading.md) - Header sections

---

## Support

For styling and layout questions:
- Review CSS documentation
- Test in browser dev tools
- Consult with your design/dev team
- Reference industry CSS practices

---

**Last Updated:** March 2026
