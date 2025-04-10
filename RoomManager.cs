using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using WatchParty.WS.Entities;

namespace WatchParty.WS
{
    public class RoomManager
    {
        private static readonly Lazy<RoomManager> _instance = new(() => new RoomManager());

        public static RoomManager Instance => _instance.Value;
        private readonly Dictionary<string, RoomContent> _rooms = [];
        private readonly Lock _lock = new();

        public async Task HandleClientAsync(string roomId, string username, Guid userId, WebSocket webSocket)
        {
            var user = await AddClientToRoomAsync(roomId, username, userId, webSocket);
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
                    else
                        await BroadcastMessageAsync(roomId, user.Name, request.Message);
                }
            }
        }

        private async Task<User> AddClientToRoomAsync(string roomId, string username, Guid userId, WebSocket webSocket)
        {
            var newUser = new User { Id = userId, WebSocket = webSocket, Name = username };

            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out RoomContent? value) || value == null)
                {
                    value = new();
                    _rooms[roomId] = value;
                }

                var userExists = value.Users.Any(u => u.Id == newUser.Id);
                if (!userExists)
                    value.Users.Add(newUser);
            }

            await BroadcastSystemAsync(roomId, $"{username} joined the room.");
            await BroadcastRoomInfoAsync(roomId);
            return newUser;
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

                return new List<string>();
            }
        }


        private async Task HandleCommandAsync(string roomId, User user, string command)
        {
            var prefix = command.Split(' ')[0];
            switch (prefix)
            {
                case "/play":
                    await BroadcastCommandAsync(roomId, user.Name, command);
                    await BroadcastSystemAsync(roomId, $"{user.Name} started the video.");
                    break;
                case "/pause":
                    await BroadcastCommandAsync(roomId, user.Name, command);
                    await BroadcastSystemAsync(roomId, $"{user.Name} paused the video.");
                    break;
                case "/queue":
                    var commandParams = GetCommandParams(command);
                    var videoUrl = TryConvertToYoutubeUrl(commandParams[0]);
                    var position = EnqueueVideoToRoom(roomId, videoUrl);
                    await BroadcastMessageAsync(roomId, user.Name, $"New video added to the queue. Current position: {position}");
                    break;
                case "/skip":
                    var nextVideo = PeekNextVideoInRoom(roomId);
                    DequeueVideoFromRoom(roomId);
                    if (nextVideo is not null)
                    {
                        await BroadcastVideoAsync(roomId, nextVideo);
                        await BroadcastMessageAsync(roomId, user.Name, $"Current video skipped. Now playing: {nextVideo}");
                    }
                    else
                        await BroadcastSystemAsync(roomId, "The video queue is empty.");
                    break;
                case "/current":
                    var currentVideo = PeekNextVideoInRoom(roomId);
                    await BroadcastMessageAsync(roomId, user.Name, $"Now playing: {currentVideo}");
                    break;
                case "/list":
                    var videos = ListAllVideosInRoom(roomId);
                    await BroadcastVideoQueueAsync(roomId, videos);
                    break;
            }
        }

        private string[] GetCommandParams(string command)
        {
            var commandParams = command.Split(' ');
            if (commandParams.Length < 2)
                return [];

            return [.. commandParams.Skip(1)];
        }

        private string TryConvertToYoutubeUrl(string url)
        {
            string youtubePattern = @"^(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?v=|embed\/|v\/|shorts\/)|youtu\.be\/)([\w\-]{11})(?:\S+)?$";
            var regex = new Regex(youtubePattern);
            if (!regex.IsMatch(url))
                return string.Empty;
            return url;
        }


        #region BROADCAST
        private async Task BroadcastVideoAsync(string roomId, string videoUrl)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "CURRENT_VIDEO",
                Message = videoUrl
            });
        }

        private async Task BroadcastVideoQueueAsync(string roomId, List<string> videos)
        {
            if (videos.Count == 0)
            {
                await BroadcastSystemAsync(roomId, "The video queue is empty.");
                return;
            }

            var formattedList = string.Join(Environment.NewLine,
                videos.Select((v, i) => $"{i + 1}. {v}"));

            var message = $"Current Video Queue:\n{formattedList}";

            await BroadcastSystemAsync(roomId, message);
        }

        private async Task BroadcastRoomInfoAsync(string roomId)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "ROOM_INFO",
                Users = GetRoomById(roomId)?.Users
            });
        }

        private async Task BroadcastCommandAsync(string roomId, string from, string command)
        {
            await BroadcastAsync(roomId, new Payload
            {
                Type = "COMMAND",
                Message = command,
                From = from
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
        #endregion
    }

    public class ClientRequest
    {
        public string Message { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class Payload
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "date")]
        public DateTime Date => DateTime.Now;

        [JsonProperty(PropertyName = "from")]
        public string? From { get; set; }

        [JsonProperty(PropertyName = "users")]
        public IEnumerable<User>? Users { get; set; }
    }
}