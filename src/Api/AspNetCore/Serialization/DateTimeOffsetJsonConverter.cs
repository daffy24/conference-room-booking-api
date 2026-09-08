using System.Text.Json;
using System.Text.Json.Serialization;
using ConferenceBooking.Api.AspNetCore.Binding;

namespace ConferenceBooking.Api.AspNetCore.Serialization;

internal sealed class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            DateTimeOffsetParser.TryParse(reader.GetString(), out var timestamp))
            return timestamp;

        throw new JsonException(DateTimeOffsetParser.ErrorMessage);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
