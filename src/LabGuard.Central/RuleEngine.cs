using System.Collections.Concurrent;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Central;

public record IncidentEvent(
    string DeviceId,
    string RuleType,
    string Severity,
    double TriggerValue,
    string Message
);

public class RuleEngine
{
    public const double HighCpuThreshold = 90.0;
    public const double HighCpuRecoverThreshold = 75.0;
    public const int HighCpuConsecutive = 3;

    public const double HighMemoryThreshold = 85.0;
    public const double HighMemoryRecoverThreshold = 70.0;
    public const int HighMemoryConsecutive = 3;

    public const int ServiceDownConsecutiveFailures = 2;
    public const int ServiceDownConsecutiveSuccesses = 2;

    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(15.0);

    private readonly TimeSpan _cooldown;
    private readonly Action<string>? _logger;

    public event Func<IncidentEvent, Task>? OnIncidentTriggered;
    public event Func<IncidentEvent, Task>? OnIncidentRecovered;

    private class DeviceRuleState
    {
        // CPU
        public int CpuHighStreak;
        public int CpuLowStreak;
        public bool CpuActive;
        public DateTime CpuLastTransitionUtc = DateTime.MinValue;

        // Memory
        public int MemoryHighStreak;
        public int MemoryLowStreak;
        public bool MemoryActive;
        public DateTime MemoryLastTransitionUtc = DateTime.MinValue;

        // Liveness
        public bool DeviceOfflineActive;
        public DateTime OfflineLastTransitionUtc = DateTime.MinValue;

        // Services: target name -> streak tracker
        public ConcurrentDictionary<string, ServiceRuleState> Services = new(StringComparer.OrdinalIgnoreCase);
    }

    private class ServiceRuleState
    {
        public int FailureStreak;
        public int SuccessStreak;
        public bool IsActive;
        public DateTime LastTransitionUtc = DateTime.MinValue;
    }

    private readonly ConcurrentDictionary<string, DeviceRuleState> _deviceStates = new(StringComparer.OrdinalIgnoreCase);

    public RuleEngine(Action<string>? logger = null, TimeSpan? cooldown = null)
    {
        _logger = logger;
        _cooldown = cooldown ?? DefaultCooldown;
    }

    private DeviceRuleState GetOrCreateState(string deviceId) =>
        _deviceStates.GetOrAdd(deviceId, _ => new DeviceRuleState());

    public bool IsIncidentActive(string deviceId, string ruleType)
    {
        if (!_deviceStates.TryGetValue(deviceId, out var state))
        {
            return false;
        }

        lock (state)
        {
            return ruleType.ToUpperInvariant() switch
            {
                "HIGH_CPU" => state.CpuActive,
                "HIGH_MEMORY" => state.MemoryActive,
                "DEVICE_OFFLINE" => state.DeviceOfflineActive,
                _ => state.Services.Values.Any(s => s.IsActive)
            };
        }
    }

    public void ProcessMetricBatch(string deviceId, double cpu, double ram)
    {
        var state = GetOrCreateState(deviceId);
        DateTime now = DateTime.UtcNow;

        lock (state)
        {
            // 1. Process CPU
            if (cpu >= HighCpuThreshold)
            {
                state.CpuHighStreak++;
                state.CpuLowStreak = 0;

                if (state.CpuHighStreak >= HighCpuConsecutive)
                {
                    if (!state.CpuActive && (now - state.CpuLastTransitionUtc >= _cooldown))
                    {
                        state.CpuActive = true;
                        state.CpuLastTransitionUtc = now;
                        _logger?.Invoke($"[RuleEngine] HIGH_CPU ACTIVE triggered for {deviceId} (CPU={cpu:F1}% >= {HighCpuThreshold}% x{state.CpuHighStreak})");
                        _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "HIGH_CPU", "HIGH", cpu, $"CPU usage reached {cpu:F1}%"));
                    }
                    else if (state.CpuActive)
                    {
                        // Still active: notify occurrence update
                        _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "HIGH_CPU", "HIGH", cpu, $"CPU usage high ({cpu:F1}%)"));
                    }
                }
            }
            else if (cpu <= HighCpuRecoverThreshold)
            {
                state.CpuLowStreak++;
                state.CpuHighStreak = 0;

                if (state.CpuActive && state.CpuLowStreak >= HighCpuConsecutive && (now - state.CpuLastTransitionUtc >= _cooldown))
                {
                    state.CpuActive = false;
                    state.CpuLastTransitionUtc = now;
                    _logger?.Invoke($"[RuleEngine] HIGH_CPU RECOVERED for {deviceId} (CPU={cpu:F1}% <= {HighCpuRecoverThreshold}% x{state.CpuLowStreak})");
                    _ = OnIncidentRecovered?.Invoke(new IncidentEvent(deviceId, "HIGH_CPU", "HIGH", cpu, $"CPU recovered to {cpu:F1}%"));
                }
            }
            else
            {
                // In hysteresis deadband [75%, 90%): reset streak towards transition
                state.CpuHighStreak = 0;
                state.CpuLowStreak = 0;
            }

            // 2. Process Memory
            if (ram >= HighMemoryThreshold)
            {
                state.MemoryHighStreak++;
                state.MemoryLowStreak = 0;

                if (state.MemoryHighStreak >= HighMemoryConsecutive)
                {
                    if (!state.MemoryActive && (now - state.MemoryLastTransitionUtc >= _cooldown))
                    {
                        state.MemoryActive = true;
                        state.MemoryLastTransitionUtc = now;
                        _logger?.Invoke($"[RuleEngine] HIGH_MEMORY ACTIVE triggered for {deviceId} (RAM={ram:F1}% >= {HighMemoryThreshold}% x{state.MemoryHighStreak})");
                        _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "HIGH_MEMORY", "HIGH", ram, $"Memory usage reached {ram:F1}%"));
                    }
                    else if (state.MemoryActive)
                    {
                        _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "HIGH_MEMORY", "HIGH", ram, $"Memory usage high ({ram:F1}%)"));
                    }
                }
            }
            else if (ram <= HighMemoryRecoverThreshold)
            {
                state.MemoryLowStreak++;
                state.MemoryHighStreak = 0;

                if (state.MemoryActive && state.MemoryLowStreak >= HighMemoryConsecutive && (now - state.MemoryLastTransitionUtc >= _cooldown))
                {
                    state.MemoryActive = false;
                    state.MemoryLastTransitionUtc = now;
                    _logger?.Invoke($"[RuleEngine] HIGH_MEMORY RECOVERED for {deviceId} (RAM={ram:F1}% <= {HighMemoryRecoverThreshold}% x{state.MemoryLowStreak})");
                    _ = OnIncidentRecovered?.Invoke(new IncidentEvent(deviceId, "HIGH_MEMORY", "HIGH", ram, $"Memory recovered to {ram:F1}%"));
                }
            }
            else
            {
                // In hysteresis deadband [70%, 85%): reset streak towards transition
                state.MemoryHighStreak = 0;
                state.MemoryLowStreak = 0;
            }
        }
    }

    public void ProcessServiceStatus(string deviceId, IEnumerable<ServiceStatusItemDto> services)
    {
        var state = GetOrCreateState(deviceId);
        DateTime now = DateTime.UtcNow;

        foreach (var svc in services)
        {
            var svcState = state.Services.GetOrAdd(svc.ServiceName, _ => new ServiceRuleState());
            lock (svcState)
            {
                if (!svc.IsListening)
                {
                    svcState.FailureStreak++;
                    svcState.SuccessStreak = 0;

                    if (svcState.FailureStreak >= ServiceDownConsecutiveFailures)
                    {
                        if (!svcState.IsActive && (now - svcState.LastTransitionUtc >= _cooldown))
                        {
                            svcState.IsActive = true;
                            svcState.LastTransitionUtc = now;
                            _logger?.Invoke($"[RuleEngine] SERVICE_DOWN ACTIVE triggered for {deviceId} service {svc.ServiceName} (failures={svcState.FailureStreak})");
                            _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "SERVICE_DOWN", "CRITICAL", svc.Port, $"Service {svc.ServiceName} (port {svc.Port}) is DOWN"));
                        }
                        else if (svcState.IsActive)
                        {
                            _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "SERVICE_DOWN", "CRITICAL", svc.Port, $"Service {svc.ServiceName} (port {svc.Port}) is DOWN"));
                        }
                    }
                }
                else
                {
                    svcState.SuccessStreak++;
                    svcState.FailureStreak = 0;

                    if (svcState.IsActive && svcState.SuccessStreak >= ServiceDownConsecutiveSuccesses && (now - svcState.LastTransitionUtc >= _cooldown))
                    {
                        svcState.IsActive = false;
                        svcState.LastTransitionUtc = now;
                        _logger?.Invoke($"[RuleEngine] SERVICE_DOWN RECOVERED for {deviceId} service {svc.ServiceName} (successes={svcState.SuccessStreak})");
                        _ = OnIncidentRecovered?.Invoke(new IncidentEvent(deviceId, "SERVICE_DOWN", "CRITICAL", svc.Port, $"Service {svc.ServiceName} recovered"));
                    }
                }
            }
        }
    }

    public void ProcessLivenessStatus(string deviceId, DeviceLivenessStatus status)
    {
        var state = GetOrCreateState(deviceId);
        DateTime now = DateTime.UtcNow;

        lock (state)
        {
            if (status == DeviceLivenessStatus.Offline)
            {
                if (!state.DeviceOfflineActive && (now - state.OfflineLastTransitionUtc >= _cooldown))
                {
                    state.DeviceOfflineActive = true;
                    state.OfflineLastTransitionUtc = now;
                    _logger?.Invoke($"[RuleEngine] DEVICE_OFFLINE ACTIVE triggered for {deviceId}");
                    _ = OnIncidentTriggered?.Invoke(new IncidentEvent(deviceId, "DEVICE_OFFLINE", "CRITICAL", 0, $"Device {deviceId} missed heartbeats > 20s"));
                }
            }
            else if (status == DeviceLivenessStatus.Online)
            {
                if (state.DeviceOfflineActive && (now - state.OfflineLastTransitionUtc >= _cooldown))
                {
                    state.DeviceOfflineActive = false;
                    state.OfflineLastTransitionUtc = now;
                    _logger?.Invoke($"[RuleEngine] DEVICE_OFFLINE RECOVERED for {deviceId}");
                    _ = OnIncidentRecovered?.Invoke(new IncidentEvent(deviceId, "DEVICE_OFFLINE", "CRITICAL", 0, $"Device {deviceId} reconnected and authenticated"));
                }
            }
        }
    }
}
