using Newtonsoft.Json;
using System;
using UnityEngine;

namespace OSK
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0;
            if (reader.TokenType == JsonToken.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string prop = reader.Value.ToString();
                        reader.Read();
                        if (prop == "x") x = Convert.ToSingle(reader.Value);
                        else if (prop == "y") y = Convert.ToSingle(reader.Value);
                        else if (prop == "z") z = Convert.ToSingle(reader.Value);
                    }
                }
            }
            return new Vector3(x, y, z);
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(value.x);
            writer.WritePropertyName("y"); writer.WriteValue(value.y);
            writer.WritePropertyName("z"); writer.WriteValue(value.z);
            writer.WritePropertyName("w"); writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0, w = 0;
            if (reader.TokenType == JsonToken.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string prop = reader.Value.ToString();
                        reader.Read();
                        if (prop == "x") x = Convert.ToSingle(reader.Value);
                        else if (prop == "y") y = Convert.ToSingle(reader.Value);
                        else if (prop == "z") z = Convert.ToSingle(reader.Value);
                        else if (prop == "w") w = Convert.ToSingle(reader.Value);
                    }
                }
            }
            return new Quaternion(x, y, z, w);
        }
    }
}
