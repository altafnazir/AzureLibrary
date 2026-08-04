using System.Threading.Tasks;

namespace AzureArchitect.Abstractions
{
    public interface IPublisher
    {
        Task PublishAsync(string topicName, object payload, System.Collections.Generic.IDictionary<string, string>? properties = null);
    }
}