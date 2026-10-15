namespace LabGuard.Agent;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("LabGuard Agent — Chương trình máy trạm sinh viên");
        Console.WriteLine("Hiện trạng hoàn thành: A-001, A-002, A-003, A-004.");
        Console.WriteLine("- A-003: Quản lý kết nối TCP & hàng đợi Single-Writer (AgentSocketClient).");
        Console.WriteLine("- A-004: Thu thập chỉ số CPU/RAM/Uptime (WindowsMetricsCollector, MetricsService, HeartbeatService).");
        Console.WriteLine("Lưu ý: Máy chủ Central (Task B-001+) chưa hiện thực, Agent đang chờ tích hợp.");
        Console.WriteLine("Nhiệm vụ tiếp theo của Sinh viên A: A-005 (Tự động kết nối lại khi mất mạng).");
        Console.WriteLine("==================================================");
    }
}
