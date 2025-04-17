using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using WatchParty.WS.Entities;
using WatchParty.WS.Models;
using WatchParty.WS.Services;

namespace WatchParty.WS.Managers
{
    public partial class RoomManager(IYoutubeService youTubeService)
    {
        private readonly Dictionary<string, RoomContent> _rooms = [];
        private readonly Lock _lock = new();
        private readonly IYoutubeService _youtubeService = youTubeService;

        public async Task HandleClientAsync(string roomId, User user)
        {
            await AddClientToRoomAsync(roomId, user);
            await ReceiveAsync(roomId, user);
            await RemoveClientFromRoomAsync(roomId, user);
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
                    else
                        await BroadcastMessageAsync(roomId, user.Name, request.Message);
                }
            }
        }

        private async Task AddClientToRoomAsync(string roomId, User user)
        {
            List<User> users;

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

                users = [.. value.Users];
            }

            await RunListUsersCommandAsync(roomId, user, "/users", silent: true);
            await BroadcastSystemAsync(roomId, $"{user.Name} joined the room.");
        }


        private async Task RemoveClientFromRoomAsync(string roomId, User user)
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
            //await RunQuitCommandAsync(roomId, user, "/quit");
        }

        private RoomContent? GetRoomById(string roomId)
        {
            lock (_lock)
            {
                _rooms.TryGetValue(roomId, out var room);
                return room;
            }
        }

        private int? EnqueueVideoToRoom(string roomId, Video video)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null)
                {
                    if (video is not null)
                    {

                        room.Videos.Enqueue(video);
                        return room.Videos.Count;
                    }
                }
                return null;
            }
        }

        private Video? DequeueVideoFromRoom(string roomId)
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

        private Video? PeekNextVideoInRoom(string roomId)
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

        private bool IsVideoQueueEmpty(string roomId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null && room.Videos.Count > 0)
                {
                    return room.Videos.Count == 0;
                }

                return true;
            }
        }

        private List<Video> ListAllVideosInRoom(string roomId)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var room) && room != null)
                {
                    return [.. room.Videos];
                }

                return [];
            }
        }
    }
}