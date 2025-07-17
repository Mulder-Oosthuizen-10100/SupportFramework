using ModularApp.Core;
using Dapper;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Text.Json;
using System.Reflection;
using System.Data;

namespace ModularApp.EngineModule
{
	public class EngineModule : IModule
	{
		private readonly DatabaseSettings _settings;
		public string Name => "EngenModule";

		public EngineModule(DatabaseSettings settings)
		{
			_settings = settings;
		}

		public async Task ExecuteAsync(string command, WebSocketConnection connection)
		{
			if (command == "get_hierarchy")
			{
				var hierarchy = await GetTableHierarchyAsync("postgresql"); // or "postgresql"
				await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, result = hierarchy }));
			}
			else
			{
				await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, error = "Unknown command" }));
			}
		}

		private async Task<List<string>> GetTableHierarchyAsync(string dbType)
		{
			var hierarchy = new List<string>();
			string sql = dbType == "mysql"
				? "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'testdb' ORDER BY TABLE_NAME"
				: "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name";

			try
			{
				using var connection = dbType == "mysql"
					? new MySqlConnection(_settings.MySqlConnectionString) as IDbConnection
					: new NpgsqlConnection(_settings.PostgreSqlConnectionString) as IDbConnection;

				//await 
					connection.Open(); // OpenAsync();
				var tables = await connection.QueryAsync<string>(sql);
				hierarchy.AddRange(tables);

				// Simple hierarchy logic: Assume tables with foreign keys depend on parent tables
				// For demo, hardcode Employee -> Department dependency
				if (hierarchy.Contains("employee") && hierarchy.Contains("department"))
				{
					hierarchy.Remove("employee");
					hierarchy.Insert(0, "employee"); // Child first
					hierarchy.Remove("department");
					hierarchy.Insert(0, "department"); // Parent first
				}
			}
			catch (Exception ex)
			{
				hierarchy.Add($"Error: {ex.Message}");
			}

			return hierarchy;
		}
	}
}