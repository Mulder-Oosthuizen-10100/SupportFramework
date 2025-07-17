namespace ModularApp.Core;

public interface IModule
{
	string Name { get; }
	Task ExecuteAsync(string command, WebSocketConnection connection); 
}
