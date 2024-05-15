using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.Json;

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float x = 0, y = 0, z = 0;

        // Ensure we are starting at a StartObject or similar token that starts the object reading process
        reader.Read(); // Read the start object or the first property name

        // Read through the properties, assuming they're in order x, y, z
        reader.Read(); // Move to property value
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "x")
        {
            reader.Read();
            x = reader.GetSingle();
        }

        reader.Read(); // Move to next property name
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "y")
        {
            reader.Read();
            y = reader.GetSingle();
        }

        reader.Read(); // Move to next property name
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "z")
        {
            reader.Read();
            z = reader.GetSingle();
        }

        reader.Read(); // Read past the end object

        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}

public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float x = 0, y = 0;

        reader.Read(); // Read the start object or the first property name

        // Read through the properties, assuming they're in order x, y
        reader.Read(); // Move to property value
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "x")
        {
            reader.Read();
            x = reader.GetSingle();
        }

        reader.Read(); // Move to next property name
        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "y")
        {
            reader.Read();
            y = reader.GetSingle();
        }

        reader.Read(); // Read past the end object

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}