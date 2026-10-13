# ImageWithContent Widget

## Overview

The `ImageWithContent` widget renders an image next to rich content and an optional CTA. It supports three layouts: image left, image right, and image stacked above content. Background and margin options are available.

## Properties

- `SelectedImages` (Content item selector) — single image required.
- `Heading` (Text input)
- `Content` (Rich text) — stored as HTML. In production prefer a rich text component; tests use a textarea.
- `CtaText` (Text input)
- `CtaUrl` (URL selector)
- `Layout` (Drop-down) — `ImageLeft`, `ImageRight`, `ImageAbove`
- `Background` (Drop-down) — `Transparent`, `Light`, `Dark`
- `TopMargin`, `BottomMargin` (Drop-down) — `None`, `Small`, `Medium`, `Large`
- `ShowCta` (Checkbox)

## Usage

Place the widget in a page using the Page Builder. Configure the image, layout and content in the editor.

## Code reference

Files:
- `Components/PageBuilder/Widgets/ImageWithContent/ImageWithContentWidget.cs` — ViewComponent, properties and view model
- `Components/PageBuilder/Widgets/ImageWithContent/ImageWithContent.cshtml` — Razor view

### Margin mapping
Top/Bottom margin enum values map to utility classes:
- `None` => `pt-0` / `pb-0`
- `Small` => `pt-1` / `pb-1`
- `Medium` => `pt-3` / `pb-3`
- `Large` => `pt-5` / `pb-5`

### Background mapping
- `Transparent` => (no class)
- `Light` => `bg-light`
- `Dark` => `bg-dark text-white`

## Notes

- The `Content` property is rendered via `@Html.Raw` in the view. Ensure content is trusted or sanitized per your security policies. Prefer using the platform's rich text editor in production.
