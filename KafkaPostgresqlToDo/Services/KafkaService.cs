using Confluent.Kafka;
using KafkaPostgresqlToDo.Kafka;
using KafkaPostgresqlToDo.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KafkaPostgresqlToDo.Services
{
    public class KafkaService
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;

        public KafkaService(
            IConfiguration configuration,
            IOptionsMonitor<KafkaOptions> options)
        {
            var kafkaConfig = new ProducerConfig
            {
                BootstrapServers = options.CurrentValue.BootstrapServers,
            };

            _producer = new ProducerBuilder<string, string>(kafkaConfig).Build();

            var consumerConfig = new ConsumerConfig
            {
                GroupId = Group.ResponseGroup.GetName(),
                BootstrapServers = options.CurrentValue.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

            _consumer.Subscribe(Topic.Responses.GetName());
        }

        public async Task ProduceAsync<TRequest>(Topic topic, TRequest request)
        {
            var message = new Message<string, string> { Value = JsonSerializer.Serialize(request) };
            await _producer.ProduceAsync(topic.GetName(), message);
        }

        public async Task<TResponse> ProduceRequestAsync<TResponse>(
            Topic topic,
            CorrelatedKafkaRequest obj)
        {
            return await ProduceRequestAsync<string, TResponse>(topic, obj);
        }

        public async Task<TResponse> ProduceRequestAsync<TRequest, TResponse>(
            Topic topic,
            CorrelatedKafkaRequest<TRequest> obj)
        {
            var message = new Message<string, string>
            {
                Key = obj.CorrelationId,
                Value = JsonSerializer.Serialize(obj)
            };

            await _producer.ProduceAsync(topic.GetName(), message);

            return await ConsumeResponseAsync<TResponse>(obj.CorrelationId);
        }

        private Task<TResponse> ConsumeResponseAsync<TResponse>(string correlationId)
        {
            while (true)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(10));

                if (consumeResult == null) continue;

                var response = JsonSerializer.Deserialize<CorrelatedKafkaResponse<TResponse>>(consumeResult?.Message?.Value);

                if (response?.CorrelationId != correlationId) continue;

                return Task.FromResult(response.GetResult!);
            }
        }
    }
}
