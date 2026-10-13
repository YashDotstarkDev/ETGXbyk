# Page Builder Widgets

## Overview
This section contains comprehensive documentation for all available Page Builder Widgets in the DotStark Xperience by Kentico Accelerator. Widgets are individual content components that can be placed within sections to build dynamic page layouts.

---

## Available Widgets

| # | Widget Name | Identifier | Purpose | Icon |
|---|-------------|-----------|---------|------|
| 1 | [Card List](./CardList.md) | `Website.CardListWidget` | Responsive grid of Card content items | L Header List Image |
| 2 | [CTA Button](./CTAButton.md) | `Website.CtaButtonWidget` | Clickable call-to-action buttons | Rectangle |
| 3 | [Embed](./Embed.md) | `Website.EmbedWidget` | Embedded HTML content (videos, iframes) | XML Tag |
| 4 | [FAQ](./FAQ.md) | `Website.FAQWidget` | Frequently asked questions in accordion format | Checklist |
| 5 | [Heading](./Heading.md) | `Website.HeadingWidget` | Text headings with multiple levels and anchor links | H Header Text |
| 6 | [Hero Banner](./Hero.md) | `Website.HeroWidget` | Full-width banner with image, title, subtitle, and CTA | Picture |
| 7 | [Image](./Image.md) | `Website.ImageWidget` | Images with sizing and theme options | Camera |
| 8 | [Image With Content](./ImageWithContent.md) | `Website.ImageWithContentWidget` | Image alongside rich text and optional CTA | Picture |
| 9 | [Notification Banner](./NotificationBanner.md) | `Website.NotificationBannerWidget` | Dismissible alert banner with scheduling | I (Info) |
| 10 | [Related Blogs](./RelatedBlogs.md) | `Website.RelatedBlogsWidget` | Blog post card grid — latest or manually selected | Checklist |
| 11 | [Swiper Slider](./Swiper.md) | `Website.SwiperWidget` | Configurable Swiper.js image/content carousel | Picture |
| 12 | [Video](./Video.md) | `Website.VideoWidget` | Video player with sizing and caption options | Triangle Right (Play) |

---

## Widget Categories

### Content Widgets
Widgets for displaying specific types of media content:
- **[Image Widget](./Image.md)** — Display images with responsive sizing and visual themes
- **[Video Widget](./Video.md)** — Embed video content with playback controls
- **[Embed Widget](./Embed.md)** — Include external HTML/iframe content
- **[Image With Content Widget](./ImageWithContent.md)** — Image alongside text and CTA

### Text & Navigation Widgets
Widgets for text, headings, and calls-to-action:
- **[Heading Widget](./Heading.md)** — Multiple heading levels with anchor links
- **[CTA Button Widget](./CTAButton.md)** — Call-to-action buttons with configurable links
- **[Notification Banner Widget](./NotificationBanner.md)** — Alert banners with scheduling and dismiss support

### Structural & Layout Widgets
Widgets for presenting groups of structured content:
- **[Hero Banner Widget](./Hero.md)** — Full-width page header with background image and CTA
- **[Card List Widget](./CardList.md)** — Responsive grid of Card content items
- **[Swiper Slider Widget](./Swiper.md)** — Configurable carousel/slider powered by Swiper.js
- **[Related Blogs Widget](./RelatedBlogs.md)** — Blog post card grid with auto or manual data source

### Complex / Interactive Widgets
Widgets with advanced behaviour:
- **[FAQ Widget](./FAQ.md)** — Expandable accordion Q&A sections
- **[Notification Banner Widget](./NotificationBanner.md)** — Date-scheduled dismissible banners

---

## Widget Properties Reference

### Common Properties
Most widgets share similar configuration options:

#### Content Selection
- **SelectedImages / SelectedVideos / SelectedEmbeds / SelectedCards** — Choose content items
- **ContentItemReference** — Reference to Xperience content items
- Typically require 1 item (minimum = 1, maximum = 1), except Card List which allows multiple

#### Display Options
- **Alignment** — Left, Center, Right positioning
- **Size** — Various sizing options (Full_Width, Half_Width, etc.)
- **Theme** — Visual styling (Shadow, Rounded_Corners, etc.)
- **Columns** — Grid column count (One, Two, Three, Four)

#### Styling
- **TopMargin / BottomMargin** — Spacing above and below the widget (None, Small, Medium, Large)
- **Padding** — Internal spacing for banner/hero widgets
- **CustomCssClass** — Optional extra CSS class for advanced styling

---

## Common Enum Values

### Alignments
- **Left** — Left alignment
- **Center** — Centre alignment
- **Right** — Right alignment (where applicable)

### Sizing
- **Full_Width** — 100% container width
- **Half_Width** — 50% container width
- **Third_Width** — 33% container width
- **Quarter_Width** — 25% container width
- **Natural** — Original dimensions

### Margin / Padding Sizes
- **None** — No spacing
- **Small** — Small spacing
- **Medium** — Medium spacing
- **Large** — Large spacing

### Column Counts
- **One** — Single column
- **Two** — Two equal columns
- **Three** — Three equal columns (typical default)
- **Four** — Four equal columns

---

## Widget Usage Guide

### Basic Widget Usage
1. Add a section to the page (e.g. Single Column, Grid)
2. Add widget to the section's widget zone
3. Configure widget properties in the admin panel
4. Widget renders on the frontend with the configured options

### Content Management
- **Widgets reference content items** stored in Xperience Content Hub
- **Create content items first**, then select them in widget properties
- **Updates automatically** when source content is republished
- **Caching enabled** for most widgets — check individual widget docs for exceptions

### Styling & Customisation
- **Widget-level properties** for common styling (margin, padding, alignment)
- **CustomCssClass** for advanced or project-specific overrides
- **Admin properties** for no-code configuration by editors
- **View templates** for developer customisation

---

## Widget Development

### Creating Custom Widgets
To add custom widgets:

1. **Create Widget Class**
   ```csharp
   [assembly: RegisterWidget(
       identifier: "Your.CustomWidget",
       name: "Custom Widget",
       viewComponentType: typeof(YourCustomWidget),
       propertiesType: typeof(YourCustomWidgetProperties),
       Description = "Description",
       AllowCache = true)]

   public class YourCustomWidget : ViewComponent { }
   ```

2. **Define Properties**
   ```csharp
   public class YourCustomWidgetProperties : BaseWidgetProperties
   {
       [TextInputComponent(Label = "Title", Order = 1)]
       public string Title { get; set; } = "";
   }
   ```

3. **Create View**
   - Add a `.cshtml` view file for rendering
   - Reference the view path in `ViewComponent.InvokeAsync()`

4. **Register Service (if needed)**
   - Add to `ServiceCollectionAppExtensions` or `ServiceCollectionXperienceExtensions`
   - Register as `Transient` for background-compatible services

---

## Widget Best Practices

1. **Always provide alt text** for images
2. **Use descriptive link text** for CTA buttons (avoid "Click here")
3. **Optimise media files** before uploading
4. **Test on multiple screen sizes** for responsiveness
5. **Use semantic heading hierarchy** (H1 → H2 → H3)
6. **Provide fallback content** for embeds
7. **Keep content concise** and scannable
8. **Use scheduling** (Notification Banner) for time-limited content

---

## Performance Considerations

### Caching
- Most widgets have caching **enabled** by default (`AllowCache = true`)
- **Notification Banner** has caching **disabled** — it evaluates a live date schedule per request
- Cache improves page load times significantly
- Cache is automatically invalidated when referenced content items change

### Media Optimisation
- **Compress images** before uploading
- **Use appropriate video codec** for faster loading
- **Lazy load** heavy content where possible (lazy loading is applied to images in most widgets)
- **Monitor page size** when combining multiple media widgets

---

## Accessibility Guidelines

### For All Widgets
- Use semantic HTML elements
- Maintain colour contrast ratios (WCAG AA minimum)
- Support keyboard navigation
- Provide text alternatives for images
- Use descriptive link text

### Image Widgets
- Populate alt text on every image content item
- Avoid images-only content for important information

### Video & Swiper Widgets
- Enable keyboard control where applicable
- Ensure player controls are keyboard accessible
- Autoplay should respect `prefers-reduced-motion`

### Heading Widgets
- Maintain logical heading hierarchy
- Use H1 only once per page
- Anchor links should be keyboard accessible

### Notification Banner
- Uses `role="alert"` for screen reader compatibility
- Dismiss button includes accessible `aria-label`

---

## Troubleshooting

### Widget Not Rendering
- Verify a content item is selected
- Check if the content item still exists in the Content Hub
- Confirm required fields (e.g. Title for Hero, cards for Card List) are populated
- Review the browser console and Kentico event log for errors

### Content Not Updating
- Clear page cache if caching is enabled
- Check content item publication status
- Verify the content item hasn't been deleted
- Rebuild the Lucene index if search-related

### Styling Issues
- Inspect with browser developer tools
- Check for custom CSS class conflicts
- Verify Bootstrap version compatibility
- Test responsive breakpoints at 375px, 768px, 1024px, 1440px

---

## Summary Table

| Widget | Caching | Properties Count | Data Source | Complex |
|--------|---------|-----------------|-------------|---------|
| Card List | ✅ Yes | 9 | Content items | Intermediate |
| CTA Button | ✅ Yes | 5 | None | Basic |
| Embed | ✅ Yes | 3 | Content item | Basic |
| FAQ | ✅ Yes | 3 | Content items / Group | Advanced |
| Heading | ✅ Yes | 5 | None | Basic |
| Hero Banner | ✅ Yes | 8 | Content item | Intermediate |
| Image | ✅ Yes | 4 | Content item | Intermediate |
| Image With Content | ✅ Yes | 9 | Content item | Intermediate |
| Notification Banner | ❌ No | 8 | None | Intermediate |
| Related Blogs | ✅ Yes | 13 | BlogPostPage items | Advanced |
| Swiper Slider | ✅ Yes | 18 | Content items / Group | Advanced |
| Video | ❌ Not specified | 4 | Content item | Intermediate |

---

## Related Documentation

- **[Page Builder Sections](../Sections/README.md)** — Learn about layout sections
- **[Xperience Documentation](https://devnet.kentico.com)** — Official Kentico docs

---

**Last Updated:** June 2026  
**Total Widgets:** 12  
**All Widgets Documented:** ✅
