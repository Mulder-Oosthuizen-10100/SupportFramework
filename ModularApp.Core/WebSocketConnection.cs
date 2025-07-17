using System.Net.WebSockets;
using System.Text;

namespace ModularApp.Core
{
	public class WebSocketConnection
	{
		private readonly WebSocket _webSocket;

		public WebSocketConnection(WebSocket webSocket)
		{
			_webSocket = webSocket;
		}

		public async Task SendAsync(string message)
		{
			var buffer = Encoding.UTF8.GetBytes(message);
			await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public async Task<string> ReceiveAsync()
		{
			var buffer = new byte[1024];
			var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			return Encoding.UTF8.GetString(buffer, 0, result.Count);
		}
	}
}
