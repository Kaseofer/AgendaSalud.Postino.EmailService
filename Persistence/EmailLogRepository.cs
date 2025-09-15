using Npgsql;
using System.Text.Json;

namespace Postino.EmailService.Persistence
{
    public class EmailLogRepository
    {
        protected readonly string _connectionString;
       public EmailLogRepository(IConfiguration configuration) 
        {

            _connectionString = configuration.GetConnectionString("AuditDb")!;
        }

        public async Task LogAsync(string entityId, string eventType, object payload)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                   INSERT INTO audit_log (entity_id, event_type, payload)
                   VALUES (@e, @t, @p::jsonb)";
            cmd.Parameters.AddWithValue("e", entityId);
            cmd.Parameters.AddWithValue("t", eventType);
            cmd.Parameters.AddWithValue("p", JsonSerializer.Serialize(payload));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}