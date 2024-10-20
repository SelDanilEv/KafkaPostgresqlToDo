using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaPostgresqlToDo.Kafka;
using KafkaPostgresqlToDo.Models;
using KafkaPostgresqlToDo.Options;
using KafkaPostgresqlToDo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KafkaPostgresqlToDo.Workers;

public class KafkaConsumerWorker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaService _kafkaService;

    public KafkaConsumerWorker(
        IServiceProvider serviceProvider,
        KafkaService kafkaService,
        IOptionsMonitor<KafkaOptions> options)
    {
        _kafkaService = kafkaService;
        _serviceProvider = serviceProvider;

        var config = new ConsumerConfig
        {
            GroupId = Group.ToDoOperations.GetName(),
            BootstrapServers = options.CurrentValue.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();

        CreateTopicsIfNotExist(
        [
            Topic.RequestToDoList.GetName(),
            Topic.CreateToDo.GetName(),
            Topic.UpdateToDo.GetName(),
            Topic.DeleteToDo.GetName(),
            Topic.Responses.GetName()
        ], options.CurrentValue.BootstrapServers).GetAwaiter().GetResult();

        _consumer.Subscribe(
        [
            Topic.RequestToDoList.GetName(),
            Topic.CreateToDo.GetName(),
            Topic.UpdateToDo.GetName(),
            Topic.DeleteToDo.GetName()
        ]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = await Task.Run(() => _consumer.Consume(stoppingToken), stoppingToken);
                await HandleMessageAsync(consumeResult, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

        var topic = consumeResult.Topic.ToTopic();

        switch (topic)
        {
            case Topic.RequestToDoList:
                var request = JsonSerializer.Deserialize<CorrelatedKafkaRequest>(
                    consumeResult.Message.Value);

                var list = await dbContext.Todos.ToListAsync();

                await _kafkaService.ProduceAsync(Topic.Responses, request.CreateResponse(list));

                return;
            case Topic.CreateToDo:
                var todoToCreate = JsonSerializer.Deserialize<Todo>(consumeResult.Message.Value);

                dbContext.Todos.Add(todoToCreate);
                break;
            case Topic.UpdateToDo:
                var todoToUpdate = JsonSerializer.Deserialize<Todo>(consumeResult.Message.Value);

                var existingTodo = await dbContext.Todos.FindAsync(todoToUpdate.Id);
                if (existingTodo != null)
                {
                    dbContext.Entry(existingTodo).CurrentValues.SetValues(todoToUpdate);
                }
                break;
            case Topic.DeleteToDo:
                var todoToDelete = JsonSerializer.Deserialize<Todo>(consumeResult.Message.Value);

                var entity = await dbContext.Todos.FindAsync(todoToDelete.Id);
                if (entity != null)
                {
                    dbContext.Todos.Remove(entity);
                }
                break;
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }


    private async Task CreateTopicsIfNotExist(string[] topics, string bootstrapServers)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var topicsToCreate = topics
                .Where(topic => !existingTopics.Contains(topic))
                .Select(topic => new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 })
                .ToList();

            if (topicsToCreate.Count != 0)
            {
                await adminClient.CreateTopicsAsync(topicsToCreate);
            }
        }
        catch (CreateTopicsException e)
        {
            Console.WriteLine($"An error occurred creating topics: {e.Results[0].Error.Reason}");
        }
    }
}
