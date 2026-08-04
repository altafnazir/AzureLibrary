using System.Collections.Generic;

namespace AzureArchitect.Abstractions
{
    public interface IMessage
    {
        string Body { get; }
        IDictionary<string, string> Properties { get; }
    }
}