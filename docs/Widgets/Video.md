# Video Widget

## Overview
The Video Widget displays video content from Xperience content items with flexible sizing, alignment, and optional caption options. It supports video assets with customizable display properties and optional description captions.

**Widget Identifier:** `Website.VideoWidget`  
**Icon:** Triangle Right (Play)  
**Caching:** Not specified in configuration  

---

## Properties

| Property | Type | Label | Description | Default Value |
|----------|------|-------|-------------|----------------|
| **SelectedVideos** | ContentItemReference | Selected video | The Video content item to display (1 item required) | [] (empty) |
| **VideoAlignment** | Enum | Alignment | Horizontal alignment of the video (Left, Center, Right) | `Center` |
| **Size** | Enum | Size | Display size of the video (Full_Width, Half_Width, Third_Width, Quarter_Width, Natural) | `Full_Width` |
| **ShowDescriptionAsCaption** | Boolean | Show description as caption? | Display video description as caption below video | `false` |

---

## Form Organization

Properties are organized in categories:
- **Content** - Selected video (Order 1-2)
- **Display** - Alignment, size, and caption options (Order 3-6)

---

## Configuration Options

### Video Alignments
- **Left** - Aligns video to the left
- **Center** - Centers the video (default)
- **Right** - Aligns video to the right

### Video Sizes
- **Full_Width** - Takes up 100% container width (default)
- **Half_Width** - Takes up 50% container width
- **Third_Width** - Takes up 33% container width
- **Quarter_Width** - Takes up 25% container width
- **Natural** - Displays at original video dimensions

### Caption Options
- **ShowDescriptionAsCaption**
  - When `true`: Video description field is displayed as caption below the video
  - When `false`: No caption displayed (default)

---

## Content Requirements

The selected Video content item must include:
- A video asset file (video file attached)
- Optional description field (for caption display)
- Optional title

---

## Error Handling

The widget displays an error message if:
- No video has been selected
- The selected content item no longer exists
- The video file no longer exists or failed to attach

---

## Data Retrieval

The widget uses MediatR and the `VideoContentByGUIDQuery` to retrieve video content, allowing for potential custom caching or processing strategies.

---

## Usage Example

1. Create or select a Video content item with a video asset
2. Add Video Widget to a page
3. Select the video from the content selector
4. Choose alignment (Left, Center, or Right)
5. Set size (Full_Width by default)
6. Enable caption option if video has a description to display

---

## Code Reference

**Component Class:** `VideoWidget`  
**Properties Class:** `VideoWidgetProperties`  
**View Model Class:** `VideoWidgetViewModel`  
**View Path:** `~/Components/PageBuilder/Widgets/Video/Video.cshtml`

**Content Type:** `VideoContent` - Contains video asset and metadata

**Query Handler:** `VideoContentByGUIDQuery` - Retrieves video by GUID

---

## Features

- **Responsive Videos** - Adapts to container sizes
- **Flexible Sizing** - Multiple predefined sizes
- **Caption Support** - Optional description captions
- **Asset Management** - Integrated with Xperience assets
- **MediatR Integration** - Extensible query handling

---

## Notes

- Exactly 1 video must be selected (minimum and maximum items = 1)
- Content item creation is allowed directly from widget selector
- Description caption is optional and controlled by checkbox
- Videos are retrieved via MediatR query pattern for extensibility
- Supports both local and external video assets

---

## Best Practices

1. **Provide descriptive titles** on video content items
2. **Include descriptions** for better context and caption generation
3. **Optimize video files** before uploading for faster loading
4. **Use Full_Width** for hero/featured videos
5. **Use captions** when video content requires context
6. **Test on different devices** to ensure responsive playback
7. **Consider video format compatibility** across browsers

---

## Accessibility Considerations

- Ensure videos have captions or transcripts
- Description captions improve context for all users
- Video player controls should be keyboard accessible
- Consider adding transcripts for important video content

---

## Related Widgets

- [Image Widget](./Image.md) - For static image content
- [Embed Widget](./Embed.md) - For embedded video services
- [Heading Widget](./Heading.md) - Combine with videos for sections

---

**Last Updated:** March 2026
