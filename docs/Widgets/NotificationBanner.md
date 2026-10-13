# Notification Banner Widget

## Overview
The Notification Banner Widget displays a configurable alert-style banner on a page. It supports four banner types (Info, Success, Warning, Error), optional rich text messages, a CTA link, a dismiss button, scheduled visibility by date range, and a custom CSS class for advanced styling.

**Widget Identifier:** `Website.NotificationBannerWidget`  
**Icon:** I (Info)  
**Caching:** Disabled (`AllowCache = false`) — caching is disabled because the banner evaluates a live date/time schedule on every request.

---

## Properties

### Content Category

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Title** | String | Title | Bold heading at the start of the banner. Leave empty to show message only. | "" (empty) |
| **Message** | Rich Text | Message | Main body of the notification. Supports inline formatting. | "" (empty) |
| **CtaText** | String | CTA Text | Label for the call-to-action link. Leave empty to hide the button. | "" (empty) |
| **CtaUrl** | URL | CTA URL | Destination URL for the CTA link/button. | "" (empty) |

### Display Category

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **Type** | Enum | Banner type | Colour and icon of the banner (Info, Success, Warning, Error) | `Info` |
| **Dismissible** | Boolean | Dismissible | Show a close button so visitors can hide the banner | `true` |
| **CustomCssClass** | String | Custom CSS class | Optional extra CSS class(es) applied to the banner wrapper | "" (empty) |

### Schedule Category

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **StartDate** | String | Start date | ISO 8601 date/time when the banner becomes visible (e.g. `2026-06-01T09:00:00`). Leave empty for no start restriction. | "" (empty) |
| **EndDate** | String | End date | ISO 8601 date/time after which the banner is hidden (e.g. `2026-12-31T23:59:59`). Leave empty for no end restriction. | "" (empty) |

---

## Configuration Options

### Banner Types
| Value | Bootstrap Class | Icon | Use Case |
|-------|----------------|------|----------|
| `Info` | `alert-info` | `bi-info-circle-fill` | General information or announcements (default) |
| `Success` | `alert-success` | `bi-check-circle-fill` | Positive confirmations, completed actions |
| `Warning` | `alert-warning` | `bi-exclamation-triangle-fill` | Cautions, upcoming changes, important notices |
| `Error` | `alert-danger` | `bi-x-circle-fill` | Critical alerts, outages, errors |

### Scheduling
- Dates are parsed as **UTC** internally
- Both `StartDate` and `EndDate` are optional
- If only `StartDate` is set: banner shows from that date indefinitely
- If only `EndDate` is set: banner shows immediately until that date
- If neither is set: banner is always visible
- In **Page Builder edit mode**, out-of-schedule banners are still shown with an editor notice
- On the **live site**, banners outside their schedule window are suppressed entirely

### Dismiss Behaviour
- When `Dismissible = true`, a close (×) button is rendered
- Dismissal state is persisted in **`localStorage`** using the widget instance ID as the key
- Dismissed banners remain hidden for the visitor until they clear their browser storage

---

## Error Handling

The widget displays an error message if both `Title` and `Message` are empty or whitespace.

---

## Usage Example

### Maintenance Window Warning
1. Add **Notification Banner Widget** to a page (e.g. the home page)
2. Set **Title** to `"Scheduled Maintenance"`
3. Set **Message** to `"Our site will be unavailable on Sunday 29 June, 2–4 AM UTC."`
4. Set **Type** to `Warning`
5. Enable **Dismissible**
6. Set **Start Date** to `2026-06-22T00:00:00`
7. Set **End Date** to `2026-06-29T04:00:00`

The banner will only display between those two dates and visitors can dismiss it.

---

## Code Reference

**Component Class:** `NotificationBannerWidget`  
**Properties Class:** `NotificationBannerWidgetProperties`  
**View Model Class:** `NotificationBannerWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/NotificationBanner/NotificationBanner.cshtml`

### View Model Computed Properties
| Property | Description |
|----------|-------------|
| `AlertClass` | Bootstrap alert CSS class (e.g. `alert-warning`) |
| `IconClass` | Bootstrap Icons class for the banner type icon |
| `AlertId` | Unique ID used for dismiss button `aria-controls` |
| `IsWithinSchedule` | `true` if the current UTC time is within the configured date window |
| `TypeLabel` | Human-readable type label for edit-mode display |

---

## Features

- **Four banner types** with matching Bootstrap colours and Bootstrap Icons
- **Rich text message** rendered via `@Html.Raw` — supports inline HTML formatting
- **Optional CTA** — button only renders when `CtaText` is non-empty
- **Dismissible** — close button with `localStorage` persistence per visitor
- **Date scheduling** — automatic show/hide based on ISO 8601 date range
- **Editor preview** — out-of-schedule banners are visible in edit mode with a notice
- **Custom CSS class** — apply project-specific styles without editing the view

---

## Notes

- Caching is intentionally **disabled** (`AllowCache = false`) — the widget must evaluate the schedule on every page request
- The `Message` field is rendered as raw HTML — ensure content is from trusted sources
- The `AlertId` is derived from the widget instance `ID` (GUID) — unique per widget placement
- Start/End dates must follow ISO 8601 format — invalid dates are silently ignored (treated as no restriction)

---

## Best Practices

1. **Always set an End date** for time-limited announcements to avoid stale banners
2. **Use Warning or Error types sparingly** — reserve for genuinely critical information
3. **Keep titles short** — the banner is a brief alert, not a content block
4. **Test scheduling** by temporarily setting a past `EndDate` and confirming the banner disappears on the live site
5. **Do not use for permanent content** — for persistent notices, consider a dedicated section instead

---

## Accessibility Considerations

- The banner uses Bootstrap's `role="alert"` pattern for screen reader compatibility
- Dismiss buttons include `aria-label` and `aria-controls` attributes
- Icon is decorative (`aria-hidden="true"`) — meaning is conveyed by text and colour

---

## Related Widgets

- [Heading Widget](./Heading.md) — Page-level headings above content areas
- [CTA Button Widget](./CTAButton.md) — Standalone call-to-action buttons

---

**Last Updated:** June 2026
