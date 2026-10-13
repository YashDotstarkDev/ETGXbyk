using System.Text.Json;
using System.Text.Json.Serialization;

namespace ETG.Core.Models.GoogleReviews;

public class ReviewListingResponse
{
    [JsonPropertyName("totals")]
    public ReviewTotals Totals { get; set; } = new();

    [JsonPropertyName("reviews")]
    public List<ReviewItem> Reviews { get; set; } = [];

    [JsonPropertyName("place_details")]
    public PlaceDetails? PlaceDetails { get; set; }
}

public class ReviewTotals
{
    // Wextractor returns these as strings e.g. "4.9" and 92
    [JsonPropertyName("average_rating")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double AverageRating { get; set; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; set; }
}

public class ReviewItem
{
    [JsonPropertyName("reviewer")]
    public string Reviewer { get; set; } = "";

    [JsonPropertyName("datetime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    // Comes back as a string e.g. "5"
    [JsonPropertyName("rating")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Rating { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class PlaceDetails
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Handles Wextractor's habit of returning numeric values as JSON strings.
/// Accepts both <c>"4.9"</c> and <c>4.9</c>.
/// </summary>
internal class StringToDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => double.TryParse(reader.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0,
            JsonTokenType.Number => reader.GetDouble(),
            _ => 0
        };

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
