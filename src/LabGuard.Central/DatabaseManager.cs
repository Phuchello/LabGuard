using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LabGuard.Central
{
    public class DatabaseManager
    {
        private readonly string _connectionString = @"Server=PC\SQLEXPRESS01;Database=LabGuardDb;Integrated Security=True;TrustServerCertificate=True;";

        public async Task TestConnectionAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                Console.WriteLine("[Database] [+] Ket noi CSDL SQL Server thanh cong!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database] [-] Loi ket noi SQL: {ex.Message}");
            }
        }

        public async Task SaveIncidentAsync(string deviceId, string ruleName, string severity)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Incidents (IncidentId, DeviceId, RuleName, ConditionState, WorkflowState, Severity)
                    VALUES (@IncidentId, @DeviceId, @RuleName, 'Active', 'New', @Severity)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IncidentId", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@DeviceId", deviceId);
                command.Parameters.AddWithValue("@RuleName", ruleName);
                command.Parameters.AddWithValue("@Severity", severity);

                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"[Database] Da luu su co tu may '{deviceId}' vao kho SQL!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database] [-] Loi luu data: {ex.Message}");
            }
        }
    }
}