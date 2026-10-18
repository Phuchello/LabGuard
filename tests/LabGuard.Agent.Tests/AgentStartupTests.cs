using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Central;
using LabGuard.Protocol;

namespace LabGuard.Agent.Tests;

public class AgentStartupTests
{
    private sealed class Files : IDisposable
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "LabGuard.Agent.Startup.Tests");
        public string DirectoryPath { get; } = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        public string PathFor(string name) => Path.Combine(DirectoryPath, name);
        public Files() => Directory.CreateDirectory(DirectoryPath);
        public void Dispose()
        {
            var relative = Path.GetRelativePath(Path.GetFullPath(Root), Path.GetFullPath(DirectoryPath));
            Assert.Matches("^[a-f0-9]{32}$", relative); // Only this test's newly created leaf, never an arbitrary path.
            Directory.Delete(DirectoryPath, recursive: true);
        }
        public string Config(int port = 5000)
        {
            var path = PathFor(Guid.NewGuid().ToString("N") + ".json");
            var settings = AgentSettings.Create(path, IPAddress.Loopback.ToString(), port);
            settings.ProbeDurationSeconds = 1;
            File.WriteAllText(path, JsonSerializer.Serialize(settings));
            return path;
        }
    }

    [Fact]
    public void InitCreatesStableIdentityAndRefusesOverwrite()
    {
        using var files = new Files(); var path = files.PathFor("first.json");
        var first = AgentSettings.Create(path, "localhost");
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(first.DeviceId, AgentSettings.Load(path).DeviceId);
        Assert.Throws<IOException>(() => AgentSettings.Create(path, "localhost"));
        Assert.Equal(bytes, File.ReadAllBytes(path));
        var second = AgentSettings.Create(files.PathFor("second.json"), "localhost");
        Assert.NotEqual(first.DeviceId, second.DeviceId);
        Assert.StartsWith("LG-", first.DeviceId);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{not-json}")]
    [InlineData("{\"DeviceId\":\"ONE\",\"deviceId\":\"TWO\",\"CentralHost\":\"localhost\"}")]
    [InlineData("{\"DeviceId\":\"ONE\",\"CentralHost\":\"localhost\",\"PreSharedToken\":\"must-not-be-in-file\"}")]
    public void InvalidOrAmbiguousJsonIsRejected(string json)
    {
        using var files = new Files(); var path = files.PathFor("invalid.json");File.WriteAllText(path,json);
        Assert.ThrowsAny<Exception>(() => AgentSettings.Load(path));
    }

    [Theory]
    [InlineData("DeviceId", "")]
    [InlineData("DeviceId", "../other")]
    [InlineData("DeviceId", "same id with spaces")]
    [InlineData("CentralHost", "http://localhost")]
    [InlineData("CentralHost", "localhost:5000")]
    [InlineData("CentralHost", " localhost")]
    [InlineData("CentralPort", "0")]
    [InlineData("CentralPort", "65536")]
    [InlineData("HeartbeatIntervalSeconds", "0")]
    [InlineData("CollectionIntervalSeconds", "-1")]
    [InlineData("ConnectTimeoutSeconds", "0")]
    [InlineData("ProbeDurationSeconds", "31")]
    [InlineData("AuthenticationTokenEnvironmentVariable", "token with spaces")]
    [InlineData("MaxLogFileBytes", "0")]
    public void InvalidSettingsFailBeforeOpeningSocket(string property,string value)
    {
        var settings = new AgentSettings { DeviceId="LG-test",CentralHost="localhost" };
        var p=typeof(AgentSettings).GetProperty(property)!;
        p.SetValue(settings,p.PropertyType==typeof(int) ? int.Parse(value) : value);
        Assert.Throws<ArgumentException>(settings.Validate);
    }

    [Fact]
    public async Task ValidationAndDefaultModeNeverOpenAnUnauthenticatedMonitoringSession()
    {
        using var files = new Files();
        await using var central=new CentralServer(0,IPAddress.Loopback);
        using var stop=new CancellationTokenSource();var running=central.StartAsync(stop.Token);
        await central.WaitForStartAsync();var connections=0;central.ClientConnected+=_=>Interlocked.Increment(ref connections);
        try
        {
            var config=files.Config(central.Port);var output=new StringWriter();var errors=new StringWriter();
            Assert.Equal(0,await AgentConsoleApplication.RunAsync(["--config",config,"--validate-config"],output,errors,default));
            Assert.Equal(5,await AgentConsoleApplication.RunAsync(["--config",config],output,errors,default));
            Assert.Contains("MonitoringNotStarted",output.ToString());
            Assert.Equal(0,Volatile.Read(ref connections));Assert.Equal("",errors.ToString());
        }
        finally { stop.Cancel();await running.WaitAsync(TimeSpan.FromSeconds(3)); }
    }

    [Fact]
    public async Task TwoConfiguredAgentsProbeActualCentralWithoutTelemetryOrAuthentication()
    {
        using var files=new Files();
        await using var central=new CentralServer(0,IPAddress.Loopback);
        using var stop=new CancellationTokenSource();var running=central.StartAsync(stop.Token);await central.WaitForStartAsync();
        var received=new ConcurrentQueue<(string Device,string Type)>();
        central.EnvelopeReceived+=(env,_)=>received.Enqueue((env.DeviceId,env.MessageType));
        try
        {
            var a=files.Config(central.Port);var b=files.Config(central.Port);
            var outputA=new StringWriter();var outputB=new StringWriter();var errorsA=new StringWriter();var errorsB=new StringWriter();
            var results=await Task.WhenAll(
                AgentConsoleApplication.RunAsync(["--config",a,"--probe"],outputA,errorsA,default),
                AgentConsoleApplication.RunAsync(["--config",b,"--probe"],outputB,errorsB,default));
            Assert.All(results,code=>Assert.Equal(0,code));
            Assert.Equal(2,received.Count);Assert.Equal(2,received.Select(x=>x.Device).Distinct().Count());
            Assert.All(received,x=>Assert.Equal(MessageTypes.HelloAgent,x.Type));
            Assert.Contains(AgentSettings.Load(a).DeviceId!,received.Select(x=>x.Device));
            Assert.Contains(AgentSettings.Load(b).DeviceId!,received.Select(x=>x.Device));
            foreach(var log in new[]{outputA.ToString(),outputB.ToString()})
            {
                Assert.Contains("TcpConnected",log);Assert.Contains("TransportClosed",log);
                Assert.Contains("\"authenticated\":false",log);
                Assert.DoesNotContain("LocalMetricsCollected",log);
            }
            Assert.Equal("",errorsA.ToString());Assert.Equal("",errorsB.ToString());
        }
        finally { stop.Cancel();await running.WaitAsync(TimeSpan.FromSeconds(3)); }
    }

    [Fact]
    public async Task CancellationDuringProbeClosesTheActualCentralConnection()
    {
        using var files=new Files();
        await using var central=new CentralServer(0,IPAddress.Loopback);
        using var stop=new CancellationTokenSource();var running=central.StartAsync(stop.Token);await central.WaitForStartAsync();
        using var cancel=new CancellationTokenSource();
        var closed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        central.EnvelopeReceived+=(_,_)=>cancel.Cancel();central.ClientDisconnected+=_=>closed.TrySetResult();
        try
        {
            var output=new StringWriter();var errors=new StringWriter();
            var code=await AgentConsoleApplication.RunAsync(["--config",files.Config(central.Port),"--probe"],output,errors,cancel.Token);
            Assert.Equal(130,code);Assert.Contains("Cancelled",output.ToString());
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally { stop.Cancel();await running.WaitAsync(TimeSpan.FromSeconds(3)); }
    }

    [Fact]
    public async Task CentralUnavailableReportsConnectionFailureWithoutRetry()
    {
        using var files=new Files();var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
        var output=new StringWriter();var errors=new StringWriter();
        Assert.Equal(4,await AgentConsoleApplication.RunAsync(["--config",files.Config(port),"--probe"],output,errors,default));
        Assert.Contains("ConnectionFailed",output.ToString());Assert.DoesNotContain("TcpConnected",output.ToString());
    }

    [Fact]
    public async Task LocalCollectionUsesWindowsCollectorWithoutNetwork()
    {
        if (!OperatingSystem.IsWindows()) return; // Separate Windows executable evidence is required by the plan.
        using var files=new Files();var output=new StringWriter();var errors=new StringWriter();
        Assert.Equal(0,await AgentConsoleApplication.RunAsync(["--config",files.Config(),"--collect-once"],output,errors,default));
        var line=output.ToString().Split(Environment.NewLine,StringSplitOptions.RemoveEmptyEntries)
            .Select(s=>JsonDocument.Parse(s)).Single(d=>d.RootElement.GetProperty("eventName").GetString()=="LocalMetricsCollected");
        using(line)
        {
            var data=line.RootElement.GetProperty("data");Assert.True(data.GetProperty("localOnly").GetBoolean());
            Assert.InRange(data.GetProperty("cpuPercent").GetDouble(),0,100);
            Assert.True(data.GetProperty("TotalPhysicalBytes").GetUInt64()>0);
        }
        Assert.DoesNotContain("TcpConnected",output.ToString());
    }

    [Fact]
    public async Task LogsNeverReadTheReferencedTokenAndFileGrowthIsBounded()
    {
        using var files=new Files();var config=files.Config();var settings=AgentSettings.Load(config);
        var key="LABGUARD_TEST_"+Guid.NewGuid().ToString("N");var secret=Guid.NewGuid().ToString();
        settings.AuthenticationTokenEnvironmentVariable=key;settings.LogFilePath="agent.log";settings.MaxLogFileBytes=1024;
        File.WriteAllText(config,JsonSerializer.Serialize(settings));Environment.SetEnvironmentVariable(key,secret);
        try
        {
            var output=new StringWriter();var errors=new StringWriter();
            Assert.Equal(5,await AgentConsoleApplication.RunAsync(["--config",config],output,errors,default));
            using(var log=new AgentLog(settings,config,output))
                for(var i=0;i<50;i++)log.Write("TestEvent","Non-secret bounded logging");
            var contents=File.ReadAllText(files.PathFor("agent.log"));
            Assert.DoesNotContain(secret,contents+output+errors);Assert.Contains("LogFileLimitReached",output.ToString());
            Assert.InRange(new FileInfo(files.PathFor("agent.log")).Length,1,1024);
        }
        finally { Environment.SetEnvironmentVariable(key,null); }
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--config")]
    [InlineData("--init-config")]
    [InlineData("--central-host")]
    public async Task CliErrorsAreClear(string option)
    {
        var output=new StringWriter();var errors=new StringWriter();
        Assert.Equal(2,await AgentConsoleApplication.RunAsync([option],output,errors,default));
        Assert.Contains("ConfigurationError",errors.ToString());
    }
}
