namespace KafkaPostgresqlToDo.Kafka;

public enum Group
{
    ToDoOperations,
    ResponseGroup
}

public static class GroupExtensions
{
    public static string GetName(this Group topic)
    {
        return topic switch
        {
            Group.ToDoOperations => "todo-operations",
            Group.ResponseGroup => "response",
            _ => throw new NotImplementedException()
        };
    }

}

