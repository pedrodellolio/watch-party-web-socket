using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using WatchParty.WS.Entities;
using WatchParty.WS.Enums;
using WatchParty.WS.Models;

namespace WatchParty.WS.Managers
{
    public partial class RoomManager
    {
        private async Task BroadcastCommandAsync(string roomId, string from, string command, string content, bool silent = false)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = ServerResponseType.COMMAND.GetDisplayName(),
                Content = [content],
                Response = command,
                From = from,
                Silent = silent
            });
        }

        private async Task BroadcastCommandAsync(string roomId, string from, string command, string[] content, bool silent = false)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = ServerResponseType.COMMAND.GetDisplayName(),
                Content = content,
                Response = command,
                From = from,
                Silent = silent
            });
        }

        private async Task BroadcastMessageAsync(string roomId, string from, string message)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = ServerResponseType.MESSAGE.GetDisplayName(),
                Message = message,
                From = from
            });
        }

        private async Task BroadcastSystemAsync(string roomId, string message)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = ServerResponseType.SYSTEM.GetDisplayName(),
                Message = message,
            });
        }

        private async Task BroadcastAsync(string roomId, Payload payload)
        {
            List<User>? clients;
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomContent? content))
                    return;

                clients = [.. content.Users];
            }

            Console.WriteLine(payload.Message);
            byte[] encodedMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            foreach (var client in clients)
            {
                if (client.WebSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.WebSocket.SendAsync(new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                                               WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        private async Task SendDirectCommandAsync(WebSocket webSocket, string command)
        {
            await SendDirectMessageAsync(webSocket, new Payload
            {
                Type = ServerResponseType.COMMAND.GetDisplayName(),
                Message = command,
            });
        }

        private async Task SendDirectSystemAsync(WebSocket webSocket, string message)
        {
            await SendDirectMessageAsync(webSocket, new Payload
            {
                Type = ServerResponseType.SYSTEM.GetDisplayName(),
                Message = message,
            });
        }

        private async Task SendDirectMessageAsync(WebSocket webSocket, Payload payload)
        {
            Console.WriteLine(payload.Message);
            byte[] encodedMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));

            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                                           WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}