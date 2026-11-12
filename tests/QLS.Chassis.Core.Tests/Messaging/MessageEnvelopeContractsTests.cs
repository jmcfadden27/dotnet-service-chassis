using Newtonsoft.Json;
using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class MessageEnvelopeContractsTests
{
    [Fact]
    public void Serialize_UsesExistingEnvelopeShape()
    {
        var envelope = new MessageEnvelope<SampleBody>
        {
            Header = new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 2, 3, 4, 5), DateTimeKind.Utc),
                CorrelationId = "corr-1"
            },
            Body = new SampleBody { Name = "alpha" }
        };

        var json = JsonConvert.SerializeObject(envelope);

        Assert.Contains("\"Header\"", json);
        Assert.Contains("\"Body\"", json);
        Assert.Contains("\"SchemaVersion\":1", json);
        Assert.Contains("\"CorrelationId\":\"corr-1\"", json);
    }

    private sealed class SampleBody
    {
        public string Name { get; set; } = string.Empty;
    }
}
