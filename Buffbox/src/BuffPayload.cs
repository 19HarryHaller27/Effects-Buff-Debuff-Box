using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Buffbox;

public sealed class BuffTypeEntry
{
    [JsonProperty("s")]
    public long StartMs { get; set; }

    [JsonProperty("e")]
    public long EndMs { get; set; }
}

public static class BuffPayloadCodec
{
    public static string Serialize(Dictionary<string, BuffTypeEntry> effects) =>
        JsonConvert.SerializeObject(effects);

    public static Dictionary<string, BuffTypeEntry> DeserializeOrEmpty(string? json)
    {
        var dict = new Dictionary<string, BuffTypeEntry>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(json))
        {
            return dict;
        }
        try
        {
            var jo = JObject.Parse(json);
            foreach (JProperty prop in jo.Properties())
            {
                if (string.IsNullOrEmpty(prop.Name))
                {
                    continue;
                }
                BuffTypeEntry? ent = prop.Value.ToObject<BuffTypeEntry>();
                if (ent is not null)
                {
                    dict[prop.Name] = ent;
                }
            }
            return dict;
        }
        catch
        {
            return dict;
        }
    }
}
