using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vereesa.Neon.Data.Models.Wowhead;

public class TodayInWowSection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string RegionId { get; set; }
    public List<TodayInWowSectionGroup> Groups { get; set; }
}

public class TodayInWowSectionGroup
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int WowExpansion { get; set; }
    public TodayInWowSectionGroupContent Content { get; set; }
}

public class TodayInWowSectionGroupContent
{
    public long Duration { get; set; }
    public long[] Upcoming { get; set; }
    public string[] UpcomingLabels { get; set; }
    public string Icons { get; set; }
    public List<TodayInWowSectionGroupContentLine> Lines { get; set; }
}

public class TodayInWowSectionGroupContentLine
{
    public string Name { get; set; }
    public string Class { get; set; }
    public long? EndingUt { get; set; }
}

public class TodayInWowSectionGroupContentLineConverter : JsonConverter<TodayInWowSectionGroupContentLine>
{
    public override TodayInWowSectionGroupContentLine? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        var content = new TodayInWowSectionGroupContentLine();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return content;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "name":
                    content.Name = reader.GetString();
                    break;
                case "class":
                    content.Class = reader.GetString();
                    break;
                case "endingUt":
                    content.EndingUt = reader.GetInt64();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        TodayInWowSectionGroupContentLine value,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }
}
