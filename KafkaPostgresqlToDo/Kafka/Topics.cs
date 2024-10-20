namespace KafkaPostgresqlToDo.Kafka;

public enum Topic
{
    RequestToDoList,
    CreateToDo,
    UpdateToDo,
    DeleteToDo,

    Responses
}

public static class TopicExtentions
{
    private class Topics
    {
        public const string RequestAllToDo = "list-todo";
        public const string CreateToDo = "create-todo";
        public const string UpdateToDo = "update-todo";
        public const string DeleteToDo = "delete-todo";
        public const string Responses = "response";
    }

    private static readonly Dictionary<Topic, string> _topicToStringMap = new()
        {
            { Topic.RequestToDoList, Topics.RequestAllToDo },
            { Topic.CreateToDo, Topics.CreateToDo },
            { Topic.UpdateToDo, Topics.UpdateToDo },
            { Topic.DeleteToDo, Topics.DeleteToDo },
            { Topic.Responses, Topics.Responses }
        };


    public static string GetName(this Topic topic)
    {
        if (_topicToStringMap.TryGetValue(topic, out var name))
        {
            return name;
        }
        throw new ArgumentException($"Unknown topic: {topic}");
    }

    public static Topic ToTopic(this string topic)
    {
        var stringToTopicMap = _topicToStringMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        if (stringToTopicMap.TryGetValue(topic, out var result))
        {
            return result;
        }
        throw new ArgumentException($"Unknown topic: {topic}");
    }
}

