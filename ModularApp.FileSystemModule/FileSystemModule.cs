using ModularApp.Core;
using System.Text.Json;

namespace ModularApp.FileSystemModule
{
	public class FileSystemModule : IModule
	{
		public string Name => "FileSystemModule";

		public async Task ExecuteAsync(string command, WebSocketConnection connection)
		{
			if (command == "list_dir")
			{
				var files = Directory.GetFiles(Directory.GetCurrentDirectory()).Take(5).ToList();
				await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, result = files }));
			}
			else
			{
				await connection.SendAsync(JsonSerializer.Serialize(new { module = Name, error = "Unknown command" }));
			}
		}
	}
}
