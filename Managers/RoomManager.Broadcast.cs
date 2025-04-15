using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WatchParty.WS.Entities;
using WatchParty.WS.Managers;
using WatchParty.WS.Models;

namespace WatchParty.WS.Managers
{
    public partial class RoomManager
    {
        private async Task BroadcastVideoAsync(string roomId, string videoUrl)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "CURRENT_VIDEO",
                Message = videoUrl
            });
        }

        private async Task BroadcastActionAsync(string roomId, string action, string data)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "ACTION",
                Message = action,
                Data = data
            });
        }

        private async Task BroadcastVideoQueueAsync(string roomId, WebSocket webSocket, List<string> videos)
        {
            if (videos.Count == 0)
            {
                await SendDirectSystemAsync(webSocket, "The video queue is empty.");
                return;
            }

            var formattedList = string.Join(Environment.NewLine,
                videos.Select((v, i) => $"{i + 1}. {v}"));

            var message = $"Current Video Queue:\n{formattedList}";

            await BroadcastSystemAsync(roomId, message);
        }

        private async Task BroadcastRoomInfoAsync(string roomId)
        {
            var room = GetRoomById(roomId);
            await BroadcastAsync(roomId, new Payload
            {
                Type = "ROOM_INFO",
                Users = room?.Users,
                Videos = room?.Videos,
                IsVideoPlaying = room?.IsVideoPlaying
                //CurrentVideoPlaybackTime = room is not null ? room.CurrentVideoPlaybackTime : 0
            });
        }

        private async Task BroadcastCommandAsync(string roomId, string from, string command, string response)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "COMMAND",
                Message = command,
                From = from,
                Response = response
            });
        }

        private async Task BroadcastMessageAsync(string roomId, string from, string message)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "BROADCAST",
                Message = message,
                From = from
            });
        }

        private async Task BroadcastSystemAsync(string roomId, string message)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "SYSTEM",
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
                Type = "COMMAND",
                Message = command,
            });
        }

        private async Task SendDirectSystemAsync(WebSocket webSocket, string message)
        {
            await SendDirectMessageAsync(webSocket, new Payload
            {
                Type = "SYSTEM",
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