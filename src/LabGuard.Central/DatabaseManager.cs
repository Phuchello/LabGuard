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
    }
}