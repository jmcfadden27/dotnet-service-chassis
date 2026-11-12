using QLS.Chassis.Core.Messaging.Propagation;

namespace QLS.Chassis.Core.Sample.Demos;

/// <summary>Adds correlation and trace headers to an outgoing HTTP request, continuing the inbound trace.</summary>
internal static class PropagationDemo
{
    public static void Run(IOutboundHeaderPropagator propagator)
    {
        Section.Title("Trace propagation");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://payments.example.com/charges/1");
        propagator.Apply(request);

        foreach (var header in request.Headers)
            Console.WriteLine($"{header.Key}: {string.Join(",", header.Value)}");

        Console.WriteLine("(the X-Ray header was built from the inbound traceparent, so both carry the same trace ID)");
    }
}
