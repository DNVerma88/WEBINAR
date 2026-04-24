using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowHub.Api.Infrastructure;

/// <summary>
/// Forces all DateTime values arriving in JSON (e.g. from a datetime-local input without
/// a timezone suffix) to be treated as UTC, preventing the Npgsql
/// "Cannot write DateTime with Kind=Unspecified to timestamptz" exception.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime());
    }
}
