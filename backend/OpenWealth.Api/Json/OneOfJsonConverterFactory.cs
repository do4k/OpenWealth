using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

namespace OpenWealth.Api.Json;

/// <summary>
/// Serializes any OneOf&lt;...&gt; by writing out whichever case is actually
/// set, using that case's own runtime type — so a OneOf-typed response looks
/// on the wire exactly like a plain object of whichever variant it holds,
/// with no wrapper or discriminator. This is what lets endpoints return a
/// real discriminated union (IntelliSense, exhaustive Match/Switch) instead
/// of object for responses that are genuinely one of several named shapes,
/// e.g. per ShareVisibility tier.
/// </summary>
public class OneOfJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeof(IOneOf).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(OneOfConverter<>).MakeGenericType(typeToConvert))!;

    private class OneOfConverter<T> : JsonConverter<T> where T : IOneOf
    {
        // Response-only: a OneOf here is always something this API sent, never something it reads back.
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException($"{typeToConvert.Name} is a response-only type and cannot be deserialized.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, value.Value?.GetType() ?? typeof(object), options);
    }
}
