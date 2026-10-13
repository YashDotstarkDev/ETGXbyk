# Swiper Slider Widget

## Overview
The Swiper Slider Widget renders a configurable image/content carousel powered by [Swiper.js](https://swiperjs.com/). It supports two data source modes (individual slides or a slides group), full Swiper.js configuration including autoplay, navigation, pagination, keyboard and mousewheel control, and multiple transition effects. Each slide can have a background image (desktop and mobile variants), title, description, and a CTA link.

**Widget Identifier:** `Website.SwiperWidget`  
**Icon:** Picture  
**Caching:** Enabled  

---

## Properties

### Content Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Label** | String | Label | Optional heading displayed above the slider | "" (empty) |
| **DataSource** | Enum | Data Source | Choose between individual slides or a slides group | `Individual` |
| **SwiperSlides** | ContentItemReference[] | Slides | Individual slide items (visible when DataSource = Individual) | [] (empty) |
| **SwiperGroup** | ContentItemReference[] | Slides Group | A slides group containing multiple slides (visible when DataSource = Group; max 1) | [] (empty) |

### Display Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Padding** | Enum | Padding | Top and bottom padding (None, Small, Medium, Large) | `Medium` |

### Swiper.js Configuration Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Loop** | Boolean | Loop | Enable continuous looping of slides | `true` |
| **InitialSlide** | Number | Initial Slide (index) | Zero-based index of the starting slide | `0` |
| **SlidesPerView** | Number | Slides Per View | Number of slides visible simultaneously | `1` |
| **SpaceBetween** | Number | Space Between (px) | Gap between slides in pixels | `0` |
| **CenteredSlides** | Boolean | Center Slides | Centre the active slide within the viewport | `false` |
| **Autoplay** | Boolean | Autoplay | Enable automatic slide progression | `false` |
| **AutoplayDelay** | Number | Autoplay Delay (ms) | Time between auto-advances in milliseconds | `5000` |
| **Navigation** | Boolean | Enable Navigation (prev/next) | Show previous/next arrow buttons | `true` |
| **Pagination** | Boolean | Enable Pagination (bullets) | Show pagination indicators | `false` |
| **PaginationType** | Enum | Pagination Type | Style of pagination indicators (Bullets, Fraction, Progressbar) | `Bullets` |
| **Scrollbar** | Boolean | Enable Scrollbar | Show a scrollbar track | `false` |
| **ScrollbarDraggable** | Boolean | Draggable Scrollbar | Allow dragging the scrollbar | `true` |
| **Keyboard** | Boolean | Keyboard Control | Enable keyboard arrow key navigation | `false` |
| **Mousewheel** | Boolean | Mousewheel Control | Enable mouse wheel scrolling through slides | `false` |
| **Effect** | String | Effect | Swiper transition effect (slide, fade, cube, coverflow, flip) | `"slide"` |
| **CustomCssClass** | String | Custom CSS Class | Optional extra CSS class applied to the slider wrapper | "" (empty) |

---

## Form Organisation

Properties are organised in categories:
- **Content** — Label, data source, slides/group selection, padding (Order 1–4)
- **Display** — All Swiper.js configuration settings (Order 10–25)

---

## Configuration Options

### Data Sources
| Value | Description |
|-------|-------------|
| `Individual` | Editor selects individual `SwiperSlide` items; displayed in selection order (default) |
| `Group` | Editor selects one `SwiperSlidesGroup`; all slides in the group are displayed automatically |

### Pagination Types
| Value | Description |
|-------|-------------|
| `Bullets` | Small dot indicators (default) |
| `Fraction` | "1 / 4" style current/total counter |
| `Progressbar` | A horizontal progress bar |

### Transition Effects
| Value | Description |
|-------|-------------|
| `slide` | Standard slide transition (default) |
| `fade` | Cross-fade between slides |
| `cube` | 3D cube rotation |
| `coverflow` | Cover-flow / carousel 3D effect |
| `flip` | 3D card flip |

### Padding Sizes
| Value | Description |
|-------|-------------|
| `None` | No top/bottom padding |
| `Small` | Small padding |
| `Medium` | Medium padding (default) |
| `Large` | Large padding |

---

## Content Requirements

### SwiperSlide (`Website.SwiperSlide`)
Each slide content item should include:
- **BasicItemTitle** — Slide heading
- **BasicItemShortDescription** — Slide body text
- **DesktopBackgroundImage** — Linked `ImageContent` for desktop screens
- **MobileBackgroundImage** — Linked `ImageContent` for mobile screens (optional)
- **CTAText** — CTA button label (optional)
- **CTALink** — Linked web page for the CTA button (optional `WebPageRelatedItem`)
- **OpenInNewTab** — Whether the CTA link opens in a new browser tab
- **CustomCSSClass** — Per-slide custom CSS class

### SwiperSlidesGroup (`Website.SwiperSlidesGroup`)
- **BasicItemTitle** — Group title (used as widget label when `ShowGroupTitle` is enabled)
- **BasicItemShortDescription** — Group description
- **SwiperSlidesGroupSwiperSlide** — Collection of linked `SwiperSlide` items

---

## Error Handling

The widget displays an error message if:
- No slides have been selected (Individual mode)
- The selected group no longer exists (Group mode)
- The selected group contains no slides (Group mode)

---

## Usage Example

### Simple Autoplay Hero Slider
1. Create 3–5 **SwiperSlide** content items with desktop background images
2. Add **Swiper Slider Widget** to a page
3. Set **Data Source** to `Individual`
4. Select the slide items
5. Enable **Autoplay** and set **Autoplay Delay** to `4000` (4 seconds)
6. Enable **Navigation** for prev/next arrows
7. Enable **Pagination** with type `Bullets`
8. Set **Effect** to `fade` for a smooth cross-fade
9. Set **Padding** to `Large`

### Grouped Slides
1. Create a **SwiperSlidesGroup** item and link multiple slides to it
2. Add **Swiper Slider Widget** to a page
3. Set **Data Source** to `Group`
4. Select the slides group

---

## Code Reference

**Component Class:** `SwiperWidget`  
**Properties Class:** `SwiperWidgetProperties`  
**View Model Class:** `SwiperWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/Swiper/Swiper.cshtml`

**Content Types:**
- `SwiperSlide` (`Website.SwiperSlide`) — Individual slide item
- `SwiperSlidesGroup` (`Website.SwiperSlidesGroup`) — Container for multiple slides

---

## Features

- **Swiper.js powered** — Industry-standard touch/swipe slider library
- **Desktop + mobile images** — Per-slide responsive image variants
- **Full Swiper.js config** — Autoplay, navigation, pagination, keyboard, mousewheel, effects
- **Two data modes** — Individual item selection or grouped slides
- **Per-slide CTA** — Optional button with internal page link resolution
- **Custom CSS** — Both widget-level and per-slide CSS class support
- **Cache Enabled** — Output cached for performance

---

## Notes

- `SlidesPerView` is clamped to a minimum of `1` in the view model
- `SpaceBetween` is clamped to a minimum of `0` in the view model
- CTA URL is resolved from the linked web page at render time — the linked page must be published
- Slides in Individual mode are displayed in editor-defined selection order
- When using Group mode, the group's `BasicItemTitle` is used as the widget label if `Label` is empty
- `Effect = "fade"` requires `SlidesPerView = 1`

---

## Best Practices

1. **Use desktop and mobile images** for each slide to avoid oversized images on small screens
2. **Keep autoplay delay ≥ 3000ms** — shorter delays frustrate users who are reading
3. **Enable navigation arrows** whenever autoplay is active so users can go back
4. **Limit slides to 5–7** — long carousels have very low engagement on later slides
5. **Use the `fade` effect** for content-heavy slides; `slide` for image-only sliders
6. **Avoid loop + fraction pagination** — the fraction counter behaves unexpectedly in loop mode
7. **Test keyboard and touch navigation** — verify accessibility and mobile usability

---

## Accessibility Considerations

- Enable **Keyboard Control** for keyboard-accessible navigation
- Ensure slide images have descriptive titles (used as alt text)
- Autoplay should pause on focus/hover — verify in Swiper.js initialisation
- Navigation buttons should have accessible labels (handled by Swiper.js defaults)

---

## Related Widgets

- [Hero Widget](./Hero.md) — Static full-width hero banner (no carousel)
- [Image Widget](./Image.md) — Single image display
- [Card List Widget](./CardList.md) — Static grid layout alternative to sliders

---

**Last Updated:** June 2026
