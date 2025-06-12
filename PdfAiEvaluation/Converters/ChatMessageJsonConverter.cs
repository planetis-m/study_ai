using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfAiEvaluator.Converters;

public class ChatMessageJsonConverter : JsonConverter<ChatMessage>
{
    public override ChatMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var role = root.GetProperty("Role").GetString();
        var text = root.GetProperty("Text").GetString();

        var chatRole = role?.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            _ => throw new JsonException($"Unknown chat role: {role}")
        };

        return new ChatMessage(chatRole, text);
    }

    public override void Write(Utf8JsonWriter writer, ChatMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Role", value.Role.Value);
        writer.WriteString("Text", value.Text);
        writer.WriteEndObject();
    }
}
