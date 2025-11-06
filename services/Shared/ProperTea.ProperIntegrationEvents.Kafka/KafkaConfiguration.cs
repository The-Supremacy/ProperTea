namespace ProperTea.ProperIntegrationEvents.Kafka;

public class KafkaConfiguration
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? ClientId { get; set; }
    public Acks Acks { get; set; } = Acks.All;
    public int MessageTimeoutMs { get; set; } = 10000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public CompressionType CompressionType { get; set; } = CompressionType.Snappy;
}

public enum Acks
{
    None = 0,
    Leader = 1,
    All = -1
}

public enum CompressionType
{
    None,
    Gzip,
    Snappy,
    Lz4,
    Zstd
}