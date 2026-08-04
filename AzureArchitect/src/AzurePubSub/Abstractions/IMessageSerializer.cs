using System.Threading.Tasks;

namespace AzureArchitect.Abstractions
{
    public interface IMessageSerializer
    {
        IMessage Serialize<T>(T payload, System.Collections.Generic.IDictionary<string, string>? properties = null);
        T? Deserialize<T>(IMessage message);
    }
}