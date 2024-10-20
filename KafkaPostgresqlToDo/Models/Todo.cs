namespace KafkaPostgresqlToDo.Models;

public class Todo
{
    public Guid Id { get; set; }
    public Metadata? Metadata { get; set; }
}

public class Metadata
{
    public string? Title { get; set; }
    public bool IsCompleted { get; set; }
    public string? AdditionalData { get; set; }
}
