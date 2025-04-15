using System.Net.WebSockets;
using WatchParty.WS.Entities;
using WatchParty.WS.Extensions;

namespace WatchParty.WS.Managers
{
    public partial class RoomManager
    {
        private async Task HandleCommandAsync(string roomId, User user, string command)
        {
            var prefix = command.Split(' ')[0];
            //await BroadcastCommandAsync(roomId, user.Name, command);
            switch (prefix)
            {
                case "/play":
                    await RunPlayCommandAsync(roomId, user, command);
                    break;
                case "/pause":
                    await RunPauseCommandAsync(roomId, user, command);
                    break;
                case "/queue":
                    await RunQueueCommandAsync(roomId, user, command);
                    break;
                case "/skip":
                    await RunSkipCommandAsync(roomId, user, command);
                    break;
                case "/current":
                    await RunCurrentCommandAsync(roomId, user, command);
                    break;
                case "/list":
                    await RunListCommandAsync(roomId, user, command);
                    break;
                case "/users":
                    await RunListUsersCommandAsync(roomId, user, command);
                    break;
                case "/quit":
                    await RunQuitCommandAsync(roomId, user, command);
                    break;
            }
        }

        private async Task RunQuitCommandAsync(string roomId, User user, string command)
        {
            await BroadcastCommandAsync(roomId, user.Name, command, [], $"{user.Name} disconnected");
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
            await user.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
        }

        private async Task RunListUsersCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is not null)
            {
                var users = room.Users.Select(u => u.Name).ToArray();
                await BroadcastCommandAsync(roomId, user.Name, command, users, $"Users at the room:");
            }
        }

        private async Task RunPlayCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            var videoUrl = PeekNextVideoInRoom(roomId);
            if (string.IsNullOrEmpty(videoUrl))
            {
                await BroadcastCommandAsync(roomId, user.Name, command, string.Empty, $"The video queue is empty.");
                return;
            }

            await BroadcastCommandAsync(roomId, user.Name, command, videoUrl, $"{user.Name} started the video.");
        }

        private async Task RunPauseCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            await BroadcastCommandAsync(roomId, user.Name, command, string.Empty, $"{user.Name} paused the video.");
        }

        private async Task RunQueueCommandAsync(string roomId, User user, string command)
        {
            var prefix = command.Split(' ')[0];
            var commandParams = command.GetCommandParams();
            if (commandParams.Length == 0)
            {
                await SendDirectSystemAsync(user.WebSocket, $"Invalid parameters passed to {prefix}.");
                return;
            }

            var videoUrl = commandParams[0].TryConvertToYoutubeUrl();
            if (string.IsNullOrEmpty(videoUrl))
            {
                await SendDirectSystemAsync(user.WebSocket, $"Invalid URL passed to {prefix}.");
                return;
            }

            var position = EnqueueVideoToRoom(roomId, videoUrl);
            await BroadcastSystemAsync(roomId, $"New video added to the queue. Current position: {position}");
        }

        private async Task RunSkipCommandAsync(string roomId, User user, string command)
        {
            string[] content = [];
            string message = "The video queue is empty.";

            DequeueVideoFromRoom(roomId);
            var nextVideo = PeekNextVideoInRoom(roomId);
            if (nextVideo is not null)
            {
                content = [.. content, nextVideo.ToString()];
                message = "Current video skipped. Now playing:";
            }

            await BroadcastCommandAsync(roomId, user.Name, command, content, message);
            // await BroadcastVideoAsync(roomId, nextVideo);
            // await BroadcastSystemAsync(roomId, $"Current video skipped. Now playing: {nextVideo}");
        }

        private async Task RunCurrentCommandAsync(string roomId, User user, string command)
        {
            string[] content = [];
            string message = "The video queue is empty.";
            var currentVideo = PeekNextVideoInRoom(roomId);
            if (currentVideo is not null)
            {
                content = [.. content, currentVideo.ToString()];
                message = "Now playing:";
            }
            await BroadcastCommandAsync(roomId, user.Name, command, content, message);
        }

        private async Task RunListCommandAsync(string roomId, User user, string command)
        {
            var videos = ListAllVideosInRoom(roomId);
            await BroadcastCommandAsync(roomId, user.Name, command, [.. videos], "Next videos:");
        }
    }
}