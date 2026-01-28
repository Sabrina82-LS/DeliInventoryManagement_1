using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace DeliInventoryManagement_1.Api.Services;

public sealed class CosmosStjSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosStjSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public override T FromStream<T>(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
            return (T)(object)stream;

        using var sr = new StreamReader(stream);
        var json = sr.ReadToEnd();

        return JsonSerializer.Deserialize<T>(json, _options)!;
    }

    public override Stream ToStream<T>(T input)
    {
        var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, leaveOpen: true);
        var json = JsonSerializer.Serialize(input, _options);
        sw.Write(json);
        sw.Flush();
        ms.Position = 0;
        return ms;
    }
}
