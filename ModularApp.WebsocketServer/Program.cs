using Microsoft.Extensions.DependencyInjection;
using ModularApp.Core;
using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ModularApp.WebSocketServer
{
	class Program
	{
		static async Task Main(string[] args)
		{
			// Setup DI
			var services = new ServiceCollection();

			services.AddSingleton(new DatabaseSettings
			{
				MySqlConnectionString = "Server=localhost;Database=testdb;User=root;Password=helloConnect56$;",
				PostgreSqlConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres;"
			});

			try
			{
				services.AddSingleton<IModule, EngineModule.EngineModule>();
				services.AddSingleton<IModule, FileSystemModule.FileSystemModule>();
				services.AddSingleton<IModule, DatabaseMigrationModule.DatabaseMigrationModule>();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Module registration failed: {ex}");
			}


			//services.AddSingleton<IModule, EngineModule.EngineModule>();
			//services.AddSingleton<IModule, FileSystemModule.FileSystemModule>();
			//services.AddSingleton<IModule, DatabaseMigrationModule.DatabaseMigrationModule>();
			var serviceProvider = services.BuildServiceProvider();


			var moduleInstances = serviceProvider.GetServices<IModule>().ToList();
			foreach ( var moduleInstance in moduleInstances )
			{
				Console.WriteLine($"Loaded module: {moduleInstance?.Name ?? "NULL"}");
            }

			var modules = moduleInstances.ToDictionary(m => m.Name, m => m);

			//var modules = serviceProvider.GetServices<IModule>().ToDictionary(m => m.Name, m => m);

			// Setup HTTP listener for WebSocket
			var listener = new HttpListener();
			listener.Prefixes.Add("http://0.0.0.0:8080/ws/");
			listener.Start();
			Console.WriteLine("WebSocket server running at ws://localhost:8080/ws/");

			while (true)
			{
				var context = await listener.GetContextAsync();
				if (context.Request.IsWebSocketRequest)
				{
					var wsContext = await context.AcceptWebSocketAsync(null);
					var ws = wsContext.WebSocket;
					var connection = new WebSocketConnection(ws);

					try
					{
						// Send available modules
						await connection.SendAsync(JsonSerializer.Serialize(new { modules = modules.Keys }));

						while (ws.State == WebSocketState.Open)
						{
							var message = await connection.ReceiveAsync();
							var request = JsonSerializer.Deserialize<Request>(message);
							if (request != null && modules.TryGetValue(request.module, out var module))
							{
								await module.ExecuteAsync(request.command, connection);
							}
							else
							{
								await connection.SendAsync(JsonSerializer.Serialize(new { error = "Invalid module or command" }));
							}
						}
					}
					catch (Exception ex)
					{
						await connection.SendAsync(JsonSerializer.Serialize(new { error = ex.Message }));
					}
					finally
					{
						await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
					}
				}
				else
				{
					context.Response.StatusCode = 400;
					context.Response.Close();
				}
			}
		}
	}

	public class Request
	{
		public string module { get; set; }
		public string command { get; set; }
	}
}