namespace KafkaPostgresqlToDo.Kafka;

public class CorrelatedKafkaRequest : CorrelatedKafkaRequest<string>
{
    public static CorrelatedKafkaRequest CreateDefault => new();
}

public class CorrelatedKafkaRequest<T>
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public T Body { get; set; } = default;

    public CorrelatedKafkaResponse<RType> CreateResponse<RType>(RType response)
    {
        return new CorrelatedKafkaResponse<RType>
        {
            CorrelationId = CorrelationId,
            Body = response
        };
    }
}

public class CorrelatedKafkaResponse<T>
{
    public string CorrelationId { get; set; }
    public T? Body { get; set; }

    public T? GetResult => Body;
}
