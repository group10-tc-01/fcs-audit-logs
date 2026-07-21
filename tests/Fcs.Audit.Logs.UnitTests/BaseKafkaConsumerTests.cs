using System.Diagnostics;
using System.Reflection;
using System.Text;
using Confluent.Kafka;
using fcs.Audit.Logs.Application.Common.Abstractions;
using FluentAssertions;
using Xunit;

namespace Fcs.Audit.Logs.UnitTests;

public sealed class BaseKafkaConsumerTests
{
    [Fact]
    public void GetParentContext_WhenTraceStateIsMissing_ReturnsTraceContext()
    {
        var headers = new Headers();
        headers.Add("traceparent", Encoding.UTF8.GetBytes("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"));

        var context = GetParentContext(headers);

        context.TraceId.ToString().Should().Be("4bf92f3577b34da6a3ce929d0e0e4736");
        context.SpanId.ToString().Should().Be("00f067aa0ba902b7");
    }

    private static ActivityContext GetParentContext(Headers headers)
    {
        var method = typeof(BaseKafkaConsumer<object>).GetMethod("GetParentContext", BindingFlags.NonPublic | BindingFlags.Static);

        return (ActivityContext)method!.Invoke(null, [headers])!;
    }
}
