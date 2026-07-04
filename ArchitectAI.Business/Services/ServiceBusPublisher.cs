using System.Text.Json;
using ArchitectAI.Business.Options;
using ArchitectAI.Business.Services.Interface;
using ArchitectAI.DomainObjects.DTOs;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchitectAI.Business.Services
{
    public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly ILogger<ServiceBusPublisher> _logger;

        public ServiceBusPublisher(
            IOptions<ServiceBusOptions> options,
            ILogger<ServiceBusPublisher> logger)
        {
            _logger = logger;
            var opt = options.Value;
            _client = new ServiceBusClient(opt.ConnectionString);
            _sender = _client.CreateSender(opt.QueueName);
        }

        public async Task PublishAssessmentRequestAsync(AssessmentQueueMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            var sbMessage = new ServiceBusMessage(json)
            {
                MessageId   = message.SessionId,
                ContentType = "application/json"
            };

            await _sender.SendMessageAsync(sbMessage);
            _logger.LogInformation("Enqueued assessment request for session {Id}", message.SessionId);
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
        }
    }

}