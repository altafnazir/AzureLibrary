using System.Collections.Generic;

namespace AzureArchitect.Common
{
    using AzureArchitect.Abstractions;

    public sealed class Message : IMessage
    {
        public Message(string body, IDictionary<string, string>? properties = null)
        {
            Body = body;
            Properties = properties ?? new Dictionary<string, string>();
        }

        public string Body { get; }
        public IDictionary<string, string> Properties { get; }
    }
}