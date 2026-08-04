using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Administration;

namespace AzurePubSub.Management
{
    public sealed class EventHubManager
    {
        private readonly EventHubAdministrationClient _admin;

        public EventHubManager(EventHubAdministrationClient admin)
        {
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
        }

        public async Task CreateEventHubIfNotExistsAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
            try
            {
                await _admin.GetEventHubPropertiesAsync(name).ConfigureAwait(false);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                await _admin.CreateEventHubAsync(name).ConfigureAwait(false);
            }
        }
    }
}