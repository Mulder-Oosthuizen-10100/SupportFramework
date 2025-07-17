using ModularApp.Core;
using Dapper;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Text.Json;

namespace ModularApp.DatabaseMigrationModule
{
	public class DatabaseMigrationModule : IModule
	{
		private readonly DatabaseSettings _settings;
		public string Name => "DatabaseMigrationModule";

		public DatabaseMigrationModule(DatabaseSettings settings)
		{
			_settings = settings;
		}

		public async Task ExecuteAsync(string command, WebSocketConnection connection)
		{
			if (command == "migrate_data")
			{
				try
				{
					using var mysqlConn = new MySqlConnection(_settings.MySqlConnectionString);
					using var pgConn = new NpgsqlConnection(_settings.PostgreSqlConnectionString);

					await mysqlConn.OpenAsync();
					await pgConn.OpenAsync();

					// Migrate Department table
					var departments = await mysqlConn.QueryAsync("SELECT * FROM Department");
					foreach (var dept in departments)
					{
						await pgConn.ExecuteAsync(
							"INSERT INTO Department (DepartmentId, Name) VALUES (@DepartmentId, @Name)",
							new { dept.Name, dept.DepartmentId});
						await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, status = $"Migrated Department {dept.Name}" }));
					}

					// Migrate Employee table
					var employees = await mysqlConn.QueryAsync("SELECT * FROM Employee");
					foreach (var emp in employees)
					{
						await pgConn.ExecuteAsync(
							"INSERT INTO Employee (EmployeeId, Name, DepartmentId) VALUES (@EmployeeId, @Name, @DepartmentId)",
							new { emp.EmployeeID, emp.Name, emp.DepertmentId } );
						await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, status = $"Migrated Employee {emp.Name}" }));
					}

					await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, result = "Migration completed" }));
				}
				catch (Exception ex)
				{
					await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, error = ex.Message }));
				}
			}
			else
			{
				await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, error = "Unknown command" }));
			}
		}
	}
}