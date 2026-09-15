using LabGuard.Agent;
using Xunit;

namespace LabGuard.Agent.Tests;

public class AgentClientTests
{
    [Fact]
    public async Task AgentSocketClient_InitialState_IsNotConnected()
    {
        await using var client = new AgentSocketClient();
        Assert.False(client.IsConnected);
        Assert.Null(client.SessionId);
    }

    [Fact]
    public async Task SendHelloAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        await using var client = new AgentSocketClient();
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendHelloAsync("TEST-PC"));
    }
}
