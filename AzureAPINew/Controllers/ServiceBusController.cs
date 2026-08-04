using Azure.Messaging.ServiceBus;
using AzureArchitect.Common;
using AzureArchitect.Extensions;
using AzureServices.Extensions;
using AzureArchitect.Facade;
using AzureServices.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Threading;

namespace AzureAPINew.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceBusController : ControllerBase
    {
        private readonly IServiceBusService _serviceBus;
        private readonly ILogger<ServiceBusController> _logger;

        public ServiceBusController(IServiceBusService serviceBus, ILogger<ServiceBusController> logger)
        {
            _serviceBus = serviceBus;
            _logger = logger;
        }

        #region Send Message

        [HttpPost("SendToQueue")]
        public async Task<IActionResult> SendToQueue(string queueName, [FromBody] ServiceBusData message)
        {
            var messageJson = JsonSerializer.Serialize(message);

            if (message == null)
                return BadRequest("Message is required.");

            try
            {
                await _serviceBus.SendMessageAsync(queueName, messageJson);
                return Accepted();
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpPost("SendBatchToQueue")]
        public async Task<IActionResult> SendBatchToQueue(string queueName, [FromBody] List<ServiceBusData> messages)
        {
            if (messages == null || messages.Count == 0)
                return BadRequest("At least one message is required.");

            var batchItems = new List<BatchMessage<ServiceBusData>>(messages.Count);

            foreach (var message in messages)
            {
                var item = new AzureServices.Entity.BatchMessage<ServiceBusData>
                {
                    Message = message
                };

                batchItems.Add(item);
            }

            try
            {
                await _serviceBus.SendMessageBatchAsync(queueName, batchItems);
                return Accepted();
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpPost("SendToTopic")]
        public async Task<IActionResult> SendToTopic(string topicName, [FromBody] ServiceBusData message)
        {
            var messageJson = JsonSerializer.Serialize(message);

            if (message == null)
                return BadRequest("Message is required.");

            try
            {
                await _serviceBus.SendMessageAsync(topicName, messageJson, new Dictionary<string, object>
                                        {
                                        { "Department", message!.Department },
                                        { "ValidDepartment", message.IsDepartmentValid() }
                                        });
                return Accepted();
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpPost("SendBatchToTopic")]
        public async Task<IActionResult> SendBatchToTopic(string topicName, [FromBody] List<ServiceBusData> messages)
        {
            if (messages == null || messages.Count == 0)
                return BadRequest("At least one message is required.");

            var batchItems = new List<BatchMessage<ServiceBusData>>(messages.Count);

            foreach (var message in messages)
            {
                var props = new Dictionary<string, object>
                            {
                                { "Department", message.Department },
                                { "ValidDepartment", message.IsDepartmentValid() }
                            };

                var item = new AzureServices.Entity.BatchMessage<ServiceBusData>
                {
                    Message = message,
                    ApplicationProperties = props,
                };

                batchItems.Add(item);
            }

            try
            {
                await _serviceBus.SendMessageBatchAsync(topicName, batchItems);
                return Accepted();
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        #endregion Send Message

        #region Receive Message

        /// <summary>
        /// Retrieve single message from subscription
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        [HttpGet("ReceiveSingleMessageFromSubscription")]
        public async Task<IActionResult> ReceiveSingleMessageFromSubscription(string topicName, string subscriptionName)
        {
            try
            {
                var msg = await _serviceBus.ReceiveSingleMessageFromSubscription<ServiceBusData>(topicName, subscriptionName);
                if (msg == null)
                    return NoContent();

                return Ok(new { Message = msg });
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve multiple messages from subscription
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        [HttpGet("ReceiveMessagesFromSubscription")]
        public async Task<IActionResult> ReceiveMessagesFromSubscription(string topicName, string subscriptionName, int maxMessages = 10)
        {
            try
            {
                var receivedMessages = await _serviceBus.ReceiveMessagesFromSubscription<ServiceBusData>(topicName, subscriptionName, maxMessages);

                if (receivedMessages == null || receivedMessages.Count == 0)
                    return NoContent();

                return Ok(new { Messages = receivedMessages });
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve single message from queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        [HttpGet("ReceiveSingleMessageFromQueue")]
        public async Task<IActionResult> ReceiveSingleMessageFromQueue(string queueName)
        {
            try
            {
                var msg = await _serviceBus.ReceiveSingleMessageFromQueue<ServiceBusData>(queueName);
                if (msg == null)
                    return NoContent();

                return Ok(new { Message = msg });
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieve multiple messages from queue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        [HttpGet("ReceiveMessagesFromQueue")]
        public async Task<IActionResult> ReceiveMessagesFromQueue(string queueName, int maxMessages = 10)
        {
            try
            {
                var receivedMessages = await _serviceBus.ReceiveMessagesFromQueue<ServiceBusData>(queueName, maxMessages);

                if (receivedMessages == null || receivedMessages.Count == 0)
                    return NoContent();

                return Ok(new { Messages = receivedMessages });
            }
            catch (EntityNotFoundException ex)
            {
                // Return 404 with a clear payload so clients know it's a configuration/missing-entity issue
                _logger.LogWarning(ex, "Entity not found while receiving messages");
                return NotFound(new { Error = ex.Message });
            }
        }

        #endregion Receive Message

    }
}
