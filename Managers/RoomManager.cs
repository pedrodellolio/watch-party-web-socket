using Newtonsoft.Json;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using WatchParty.WS.Entities;
using WatchParty.WS.Models;

namespace WatchParty.WS.Managers
{
    public partial class RoomManager
    {
        private static readonly Lazy<RoomManager> _instance = new(() => new RoomManager());

        public static RoomManager Instance => _instance.Value;
        private readonly Dictionary<string, RoomContent> _rooms = [];
        private readonly Lock _lock = new();

        public async Task HandleClientAsync(string roomId, User user)
        {
            await AddClientToRoomAsync(roomId, user);
            await ReceiveAsync(roomId, user);
            RemoveClientFromRoom(roomId, user);
        }

        private async Task ReceiveAsync(string roomId, User user)
        {
            var buffer = new byte[4 * 1024];
            while (user.WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await user.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }


                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Disconnecting");
                    await user.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var request = JsonConvert.DeserializeObject<ClientRequest>(message);
                    if (request is null)
                        continue;

                    if (request.Action == "keep_alive")
                        Console.WriteLine(request.Message);
                    else if (request.Action == "command")
                        await HandleCommandAsync(roomId, user, request.Message);
                    else if (request.Action == "feedback")
                        HandlePlaybackTime(roomId, request.Message);
                    else
                        await BroadcastMessageAsync(roomId, user.Name, request.Message);
                }
            }
        }

        private void HandlePlaybackTime(string roomId, string time)
        {
            lock (_lock)
            {
                Console.WriteLine(time);
                if (_rooms.TryGetValue(roomId, out var room) && room != null)
                    room.CurrentVideoPlaybackTime = Convert.ToDouble(time, CultureInfo.InvariantCulture);
            }
        }

        private async Task AddClientToRoomAsync(string roomId, User user)
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomContent? value) || value == null)
                {
                    value = new();
                    _rooms[roomId] = value;
                }

                var userExists = value.Users.Any(u => u.Id == user.Id);
                if (!userExists)
                    value.Users.Add(user);
            }

            await BroadcastSystemAsync(roomId, $"{user.Name} joined the room.");
            await BroadcastRoomInfoAsync(roomId);
        }

        private void RemoveClientFromRoom(string roomId, User user)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var content))
                {
                    content.Users.Remove(user);
                    if (content.Users.Count == 0)
                    {
                        _rooms.Remove(roomId);
                    }
                }
            }
        }

        private RoomContent? GetRoomById(string roomId)
        {
            lock (_lock)
            {
                _rooms.TryGetValue(roomId, out var room);
                return room;
            }
        }

        private int? EnqueueVideoToRoom(string roomId, string video)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null)
                {
                    if (!string.IsNullOrEmpty(video))
                    {
                        room.Videos.Enqueue(video);
                        return room.Videos.Count;
                    }
                }
                return null;
            }
        }

        private string? DequeueVideoFromRoom(string roomId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null && room.Videos.Count > 0)
                {
                    return room.Videos.Dequeue();
                }

                return null;
            }
        }

        private string? PeekNextVideoInRoom(string roomId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null && room.Videos.Count > 0)
                {
                    return room.Videos.Peek();
                }

                return null;
            }
        }

        private List<string> ListAllVideosInRoom(string roomId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null)
                {
                    return room.Videos.ToList();
                }

                return [];
            }
        }
    }
}