using System.Text.Json;
using AzureArchitect.Abstractions;
using AzureArchitect.Common;
using System.Collections.Generic;

namespace AzureArchitect.Serialization
{
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonMessageSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public IMessage Serialize<T>(T payload, IDictionary<string, string>? properties = null)
        {
            var json = JsonSerializer.Serialize(payload, _options);
            return new Message(json, properties);
        }

        public T? Deserialize<T>(IMessage message)
        {
            if (message == null) return default;
            return JsonSerializer.Deserialize<T>(message.Body, _options);
        }
    }
}