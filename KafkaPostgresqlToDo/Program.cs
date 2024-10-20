using KafkaPostgresqlToDo.Kafka;
using KafkaPostgresqlToDo.Models;
using KafkaPostgresqlToDo.Options;
using KafkaPostgresqlToDo.Services;
using KafkaPostgresqlToDo.Workers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(nameof(KafkaOptions)));

// Database connection string
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TodoDatabase")));

builder.Services.AddSingleton<KafkaService>();

builder.Services.AddHostedService<KafkaConsumerWorker>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/todos", async (KafkaService kafkaService) =>
{
    return await kafkaService.ProduceRequestAsync<List<Todo>>(
        Topic.RequestToDoList, CorrelatedKafkaRequest.CreateDefault);
});

app.MapPost("/todos", async (Todo todo, KafkaService kafkaService) =>
{
    todo.Id = Guid.NewGuid();
    await kafkaService.ProduceAsync(Topic.CreateToDo, todo);
    return Results.Accepted();
});

app.MapPut("/todos/{id}", async (Todo inputTodo, KafkaService kafkaService) =>
{
    await kafkaService.ProduceAsync(Topic.UpdateToDo, inputTodo);
    return Results.Accepted();
});

app.MapPut("/todos/{id}/complete", async (Guid id, TodoDbContext db, KafkaService kafkaService) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo == null)
    {
        return Results.NotFound();
    }

    todo.Metadata.IsCompleted = true;
    await kafkaService.ProduceAsync(Topic.UpdateToDo, todo);
    return Results.Accepted();
});

app.MapDelete("/todos/{id}", async (Guid id, KafkaService kafkaService) =>
{
    var todo = new Todo { Id = id };
    await kafkaService.ProduceAsync(Topic.DeleteToDo, todo);
    return Results.Accepted();
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.Run();
