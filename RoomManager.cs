using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace WatchParty.WS
{
    public class RoomManager
    {
        private static readonly Lazy<RoomManager> _instance = new(() => new RoomManager());

        public static RoomManager Instance => _instance.Value;
        private readonly Dictionary<string, List<WebSocket>> _rooms = [];
        private readonly Lock _lock = new();

        public async Task HandleClientAsync(string roomId, WebSocket webSocket)
        {
            await AddClientToRoomAsync(roomId, webSocket);
            await ReceiveAsync(roomId, webSocket);
            RemoveClientFromRoom(roomId, webSocket);
        }

        private async Task AddClientToRoomAsync(string roomId, WebSocket webSocket)
        {
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out List<WebSocket>? value))
                {
                    value = [];
                    _rooms[roomId] = value;
                }

                value.Add(webSocket);
            }

            var payload = new
            {
                type = "BROADCAST",
                message = "Joined the room"
            };
            Console.WriteLine(payload.message);
            await BroadcastMessageAsync(roomId, JsonConvert.SerializeObject(payload));
        }

        private void RemoveClientFromRoom(string roomId, WebSocket webSocket)
        {
            lock (_lock)
            {
                if (_rooms.TryGetValue(roomId, out var clients))
                {
                    clients.Remove(webSocket);
                    if (clients.Count == 0)
                    {
                        _rooms.Remove(roomId);
                    }
                }
            }
        }

        private async Task ReceiveAsync(string roomId, WebSocket webSocket)
        {
            var buffer = new byte[4 * 1024];
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro na recepção: {ex.Message}");
                    break;
                }


                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Encerrando conexão");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Encerrando conexão", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var data = JsonConvert.DeserializeObject<Request>(message);
                    if (data is null)
                        continue;

                    if (data.Action == "keep_alive")
                    {
                        Console.WriteLine(data.Message);
                        continue;
                    }

                    var payload = new
                    {
                        type = "BROADCAST",
                        message = data.Message
                    };
                    Console.WriteLine(payload.message);
                    await BroadcastMessageAsync(roomId, message);
                }
            }
        }

        private async Task BroadcastMessageAsync(string roomId, string message)
        {
            List<WebSocket> clients;
            lock (_lock)
            {
                if (!_rooms.TryGetValue(roomId, out clients))
                    return;

                clients = [.. clients];
            }

            byte[] encodedMessage = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                                               WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
                    }
                }
            }
        }
    }

    public class Request
    {
        public string Message { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}