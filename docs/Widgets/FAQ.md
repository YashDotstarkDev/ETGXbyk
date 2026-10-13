# FAQ Widget

## Overview
The FAQ (Frequently Asked Questions) Widget displays Q&A content in an expandable accordion format. It supports two data source modes: individual FAQ items or FAQ groups, allowing flexible content organization and reusability.

**Widget Identifier:** `Website.FAQWidget`  
**Icon:** Checklist  
**Caching:** Enabled  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **DataSource** | Enum | Data Source | Choose between individual items or a FAQ group (FAQ_Group, Individual_Items) | `Individual_Items` |
| **FAQItems** | ContentItemReference[] | FAQ Items | Selected individual FAQ items (visible when DataSource = Individual_Items) | [] (empty) |
| **FAQGroup** | ContentItemReference[] | FAQ Group | Selected FAQ group containing multiple items (visible when DataSource = FAQ_Group) | [] (empty) |

---

## Configuration Options

### Data Source Types

#### FAQ_Group
- Select a single FAQ Group content item
- All FAQ items within the group are automatically displayed
- Maintains group association and organization
- Recommended for large FAQ sections

#### Individual_Items
- Manually select specific FAQ items
- Full control over which items appear and their order
- Items are displayed in the order selected
- Recommended for curated, smaller FAQ lists

---

## Content Requirements

### FAQ Item Content Type
Each FAQ item should include:
- Question field
- Answer field (typically rich text)

### FAQ Group Content Type
- Must have linked FAQ items (supports up to 1 level of linked content)
- Maintains relationships with multiple FAQ items

---

## Error Handling

The widget displays an error message if:
- No FAQ items are available (empty list)
- When using FAQ Group mode, the selected group no longer exists
- When using FAQ Group mode, the group has no linked FAQ items
- When using Individual Items mode, a selected item no longer exists

---

## Usage Example

### Using Individual Items
1. Add FAQ Widget to page
2. Set DataSource to "Individual_Items"
3. Select specific FAQ items from the content selector
4. Items appear in accordion format in selection order

### Using FAQ Group
1. Add FAQ Widget to page
2. Set DataSource to "FAQ_Group"
3. Select a FAQ Group content item
4. All linked FAQ items automatically display in the group's order

---

## Code Reference

**Component Class:** `FAQWidget`  
**Properties Class:** `FAQWidgetProperties`  
**View Model Class:** `FAQWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/FAQ/FAQ.cshtml`

**Content Types:**
- `FAQItemContent` - Individual FAQ question/answer item
- `FAQGroupContent` - Container for multiple FAQ items

---

## Features

- **Accordion Display** - Items expand/collapse on click
- **Cache Dependency** - Automatically tracks content changes
- **Flexible Organization** - Support both grouped and individual items
- **Order Preservation** - Respects selection order when using individual items
- **Linked Content Support** - Groups support nested FAQ items

---

## Notes

- Cache dependency collection is automatically managed
- The widget validates content availability before rendering
- FAQ Groups can link to multiple FAQ items (LinkedItemsMaxLevel = 1)
- Both data source modes support dynamic content updates
- Caching is enabled for improved performance

---

## Best Practices

1. **Use FAQ Groups** for comprehensive FAQ sections to maintain organization
2. **Use Individual Items** for targeted, page-specific Q&A
3. Ensure FAQ items have both question and answer content before publishing
4. Test accordion functionality with various content lengths
5. Keep questions concise and answers scannable

---

## Related Widgets

- [Heading Widget](./Heading.md) - Use for FAQ section titles
- [Embed Widget](./Embed.md) - Combine for media-rich answers

---

**Last Updated:** March 2026
