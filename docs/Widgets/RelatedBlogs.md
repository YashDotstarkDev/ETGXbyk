# Related Blogs Widget

## Overview
The Related Blogs Widget displays a responsive grid of blog post cards. It supports two data source modes — **Latest** (automatically shows the most recent published posts) and **Manual** (editor-selected posts in a defined order). Each card renders a teaser image, title, published date, author, category tags, short description, and a "Read more" CTA.

**Widget Identifier:** `Website.RelatedBlogsWidget`  
**Icon:** Checklist  
**Caching:** Enabled  

---

## Properties

### Content Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Heading** | String | Section heading | Optional heading displayed above the blog card grid | `"Related Posts"` |
| **DataSource** | Enum | Data source | How posts are selected — latest published or manually chosen | `Latest` |
| **SelectedPosts** | ContentItemReference[] | Selected posts | Specific blog posts to display (visible when DataSource = Manual). Order is preserved. | [] (empty) |
| **MaxPosts** | Number | Max posts | Maximum number of posts to display in Latest mode (recommended: 3–12) | `3` |

### Display Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Columns** | Enum | Columns | Number of columns in the card grid (One, Two, Three, Four) | `Three` |
| **ShowImage** | Boolean | Show teaser image | Display the post's teaser image at the top of each card | `true` |
| **ShowAuthor** | Boolean | Show author | Display the author's name and avatar on each card | `true` |
| **ShowDate** | Boolean | Show published date | Display the post's published date on each card | `true` |
| **ShowTags** | Boolean | Show tags | Display the blog category tags on each card | `true` |
| **ShowDescription** | Boolean | Show short description | Display the post's short description below the title | `true` |
| **CardStyle** | Enum | Card style | Visual style for each post card (WithBorder, Minimal) | `WithBorder` |
| **TopMargin** | Enum | Top margin | Spacing above the widget (None, Small, Medium, Large) | `None` |
| **BottomMargin** | Enum | Bottom margin | Spacing below the widget (None, Small, Medium, Large) | `None` |

---

## Configuration Options

### Data Sources
| Value | Description |
|-------|-------------|
| `Latest` | Automatically fetches the most recently published blog posts, sorted newest-first (default) |
| `Manual` | Editor selects specific blog post pages; display order follows the selection order |

> **Note:** When `DataSource = Manual`, the `SelectedPosts` selector is shown and `MaxPosts` is hidden. When `DataSource = Latest`, `MaxPosts` is shown and `SelectedPosts` is hidden.

### Columns
| Value | Bootstrap Class | Description |
|-------|----------------|-------------|
| `One` | `col-12` | Full-width single column |
| `Two` | `col-12 col-sm-6` | Two columns from small breakpoint |
| `Three` | `col-12 col-sm-6 col-lg-4` | Three columns from large breakpoint (default) |
| `Four` | `col-12 col-sm-6 col-lg-3` | Four columns from large breakpoint |

### Card Styles
| Value | Description |
|-------|-------------|
| `WithBorder` | Card with Bootstrap border and drop shadow (default) |
| `Minimal` | Flush card with no border — borderless/ghost style |

### Margin Sizes
- **None** — No spacing (default)
- **Small** — Small spacing
- **Medium** — Medium spacing
- **Large** — Large spacing

---

## Content Requirements

### BlogPostPage (`Website.BlogPostPage`)
Each blog post page must have a linked `BlogPostContent` item containing:
- **BasicItemTitle** — Card title
- **BasicItemShortDescription** — Card description (used when `ShowDescription = true`)
- **BlogPostContentPublishedDate** — Displayed when `ShowDate = true`
- **BlogPostContentTeaserMediaFileImage** — Linked `ImageContent` item (used when `ShowImage = true`)
- **BlogPostContentAuthor** — Linked `AuthorContent` item (used when `ShowAuthor = true`)
- **BlogPostContentBlogCategory** — Taxonomy tags (used when `ShowTags = true`)

---

## Error Handling

The widget displays an error message if:
- No blog posts are found for the configured channel
- In Manual mode, none of the selected posts have associated blog content items

---

## Technical Details

### Data Retrieval
- **Latest mode** uses `RelatedBlogsQuery` (MediatR) which fetches all posts for the channel and sorts newest-first in-memory
- **Manual mode** uses `RelatedBlogsManualQuery` which fetches posts by `ContentItemGUID` and preserves editor-defined order

> **Important:** `ContentItemReference.Identifier` stores the `ContentItemGUID` — not the `WebPageItemGUID`. The manual query correctly filters on `ContentItemGUID`.

### Ordering in Latest Mode
Sorting by `BlogPostContentPublishedDate` is done **in-memory** after fetching, because `BlogPostContentPublishedDate` is a field on the linked `BlogPostContent` reusable item — it is not available as a SQL column on the `BlogPostPage` table and cannot be used in `ORDER BY`.

### Tag Resolution
Tags are resolved via `ITaxonomyRetriever.RetrieveTags()` only when `ShowTags = true` and the post has category tags. Tag resolution happens per-post at render time.

### Linked Items Depth
The query uses `WithLinkedItems(4)` to cover the full chain:  
`BlogPostPage → BlogPostContent → AuthorContent → AuthorPhoto (ImageContent)`

---

## Usage Example

### Latest Posts (Automatic)
1. Add **Related Blogs Widget** to a page
2. Set **Data source** to `Latest`
3. Set **Max posts** to `3`
4. Set **Columns** to `Three`
5. Enable all display toggles
6. Set **Card style** to `WithBorder`

### Curated Selection (Manual)
1. Add **Related Blogs Widget** to a page
2. Set **Data source** to `Manual`
3. Click **Selected posts** and choose specific blog post pages
4. The selection order determines the display order
5. Configure display properties as needed

---

## Code Reference

**Component Class:** `RelatedBlogsWidget`  
**Properties Class:** `RelatedBlogsWidgetProperties`  
**View Model Class:** `RelatedBlogsWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/RelatedBlogs/RelatedBlogs.cshtml`

**Query Records:**
- `RelatedBlogsQuery` — Latest posts query (MediatR)
- `RelatedBlogsManualQuery` — Specific posts by ContentItemGUID

**Content Types:**
- `BlogPostPage` (`Website.BlogPostPage`) — Web page
- `BlogPostContent` (`Website.BlogPostContent`) — Reusable content item

---

## Features

- **Two data source modes** — automatic latest or curated manual selection
- **Full blog card** — image, title, date, author, tags, description, CTA
- **Tag display** — resolved from Kentico taxonomy at render time
- **Responsive grid** — 1–4 column Bootstrap layout
- **Card style options** — bordered or minimal
- **Conditional rendering** — each card element can be individually toggled

---

## Best Practices

1. Use **Latest** mode for blog landing pages to always show the freshest content
2. Use **Manual** mode for curated sections (e.g. "Featured Articles" on a product page)
3. Set **MaxPosts** to a multiple of your column count (e.g. 3 posts for 3 columns)
4. Ensure all blog posts have a **teaser image** for visual consistency
5. Keep **short descriptions** under 150 characters for clean card layouts
6. Use **WithBorder** card style for cards on white backgrounds; **Minimal** on coloured backgrounds

---

## Related Widgets

- [Hero Widget](./Hero.md) — Full-width hero banner above blog listing areas
- [Card List Widget](./CardList.md) — Generic card grid for non-blog content
- [Heading Widget](./Heading.md) — Section headings above blog grids

---

**Last Updated:** June 2026
