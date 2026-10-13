# Hero Banner Widget

## Overview
The Hero Banner Widget displays a full-width banner with a background image, title, optional subtitle, and a call-to-action button. It supports text alignment, image overlay, and configurable padding — making it ideal for page headers and landing sections.

**Widget Identifier:** `Website.HeroWidget`  
**Icon:** Picture  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Title** | String | Title | Main heading displayed in the hero banner (required) | "" (empty) |
| **Subtitle** | String | Subtitle | Supporting text displayed below the title | "" (empty) |
| **SelectedImage** | ContentItemReference[] | Selected image | Background image for the hero (1 item required) | [] (empty) |
| **CTAButtonText** | String | CTA text | Label for the call-to-action button | "" (empty) |
| **CTAUrl** | String | CTA URL | Destination URL for the CTA button | "" (empty) |
| **TextAlignment** | Enum | Text alignment | Horizontal alignment of title and subtitle (Left, Center, Right) | `Center` |
| **Overlay** | Enum | Image overlay | Optional dark overlay for improved text contrast (None, Dark_25, Dark_50) | `None` |
| **Padding** | Enum | Padding | Top and bottom padding size (None, Small, Medium, Large) | `Medium` |

---

## Form Organisation

Properties are organised in categories:
- **Content** — Title, subtitle, image, and CTA (Order 1–5)
- **Display** — Text alignment, overlay, and padding (Order 6–8)

---

## Configuration Options

### Text Alignments
| Value | Description |
|-------|-------------|
| `Left` | Left-aligned title and subtitle |
| `Center` | Centre-aligned title and subtitle (default) |
| `Right` | Right-aligned title and subtitle |

### Image Overlay
| Value | Description |
|-------|-------------|
| `None` | No overlay — image displayed as-is (default) |
| `Dark_25` | 25% dark overlay for mild contrast improvement |
| `Dark_50` | 50% dark overlay for strong contrast on bright images |

### Padding Sizes
| Value | CSS Applied |
|-------|-------------|
| `None` | No top/bottom padding |
| `Small` | Small padding |
| `Medium` | Medium padding (default) |
| `Large` | Large padding |

---

## Content Requirements

### Image Content Type (`Website.ImageContent`)
The selected image item must include:
- **ImageContentAsset** — The actual image file (required)
- **ThumbnailImageContentAsset** — Optional thumbnail variant
- **BasicItemTitle** — Used as alt text

---

## Error Handling

The widget displays an error message if:
- **Title** is empty or whitespace
- No image has been selected
- The selected image content item no longer exists
- The image file asset is missing or null

---

## Usage Example

1. Create or select an **ImageContent** item with a suitable background image
2. Add the **Hero Banner Widget** to a page (typically at the top of a section)
3. Enter a compelling **Title** (required)
4. Optionally add a **Subtitle** for supporting context
5. Set **CTA text** and **CTA URL** to add a button
6. Choose **Text alignment** — Centre is recommended for full-width heroes
7. Apply an **overlay** if text readability on the image is poor
8. Set appropriate **Padding** for visual breathing room

---

## Code Reference

**Component Class:** `HeroWidget`  
**Properties Class:** `HeroWidgetProperties`  
**View Model Class:** `HeroWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/Hero/Hero.cshtml`

**Content Type:** `ImageContent` (`Website.ImageContent`)

---

## Features

- **Full-Width Layout** — Designed for impactful page headers
- **Overlay Support** — Dark overlays improve text legibility on bright images
- **Optional CTA** — CTA button only renders when both text and URL are provided
- **Flexible Alignment** — Left, centre, or right text positioning
- **Configurable Padding** — Controls vertical breathing space
- **Cache Enabled** — Output cached for performance

---

## Notes

- Both **Title** and an **image** are required — the widget will not render without them
- The CTA button only renders when `CTAButtonText` is non-empty
- Exactly 1 image must be selected (MinimumItems = 1, MaximumItems = 1)
- Images can be created directly from the content item selector

---

## Best Practices

1. **Use high-resolution images** — hero images are full-width and prominent
2. **Apply Dark_25 or Dark_50 overlay** on light/busy images to ensure text readability
3. **Keep titles concise** — 5–10 words maximum for impact
4. **Use Centre alignment** for symmetrical full-width heroes
5. **Pair with a strong CTA** to direct visitors to the next step
6. **Avoid small padding** on mobile — test vertical spacing at all breakpoints

---

## Accessibility Considerations

- Alt text is sourced from `BasicItemTitle` on the image content item — always populate it
- CTA links should have descriptive text (avoid "Click here")
- Ensure sufficient colour contrast between text and overlay

---

## Related Widgets

- [Card List Widget](./CardList.md) — Feature grids below hero banners
- [Heading Widget](./Heading.md) — Section headings within page content
- [CTA Button Widget](./CTAButton.md) — Standalone call-to-action buttons

---

**Last Updated:** June 2026
