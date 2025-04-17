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
                    //case "/quit":
                    //    await RunQuitCommandAsync(roomId, user, command);
                    //    break;
            }
        }

        //private async Task RunQuitCommandAsync(string roomId, User user, string command)
        //{
        //    //await BroadcastCommandAsync(roomId, user.Name, command, [], $"{user.Name} disconnected");
        //    lock (_lock)
        //    {
        //        if (_rooms.TryGetValue(roomId, out var content))
        //        {
        //            content.Users.Remove(user);
        //            if (content.Users.Count == 0)
        //            {
        //                _rooms.Remove(roomId);
        //            }
        //        }
        //    }
        //    await user.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
        //}

        private async Task RunListUsersCommandAsync(string roomId, User user, string command, bool silent = false)
        {
            var room = GetRoomById(roomId);
            if (room is not null)
            {
                var users = room.Users.Select(u => u.Name).ToArray();
                await BroadcastCommandAsync(roomId, user.Name, command, users);
                var message = $"You are the only one in this room";
                if (users.Length > 1)
                    message = $"There are {users.Length} user(s) in this room.";
                await BroadcastSystemAsync(roomId, message);
            }
        }

        private async Task RunPlayCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is null)
                throw new ApplicationException("Room not found");

            // if room.CurrentVideoPlayingUrl is empty, it must send the videoUrl to start
            // if room.CurrentVideoPlayingUrl is NOT empty, it must just echo the command
            var videoUrl = string.Empty;
            if (string.IsNullOrEmpty(room.CurrentVideoPlayingUrl))
            {
                videoUrl = PeekNextVideoInRoom(roomId);
                room.CurrentVideoPlayingUrl = videoUrl ?? string.Empty;
            }

            await BroadcastCommandAsync(roomId, user.Name, command, videoUrl ?? string.Empty);
            await BroadcastSystemAsync(roomId, $"{user.Name} started the video.");
        }

        private async Task RunPauseCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is null)
                throw new ApplicationException("Room not found");

            await BroadcastCommandAsync(roomId, user.Name, command, string.Empty);
            await BroadcastSystemAsync(roomId, $"{user.Name} paused the video.");
        }

        private async Task RunQueueCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is null)
                throw new ApplicationException("Room not found");

            var prefix = command.Split(' ')[0];
            var commandParams = command.GetCommandParams();
            if (commandParams.Length == 0)
            {
                await BroadcastSystemAsync(roomId, $"Invalid parameters passed to {prefix}.");
                return;
            }

            var videoUrl = commandParams[0].TryConvertToYoutubeUrl();
            if (string.IsNullOrEmpty(videoUrl))
            {
                await SendDirectSystemAsync(user.WebSocket, $"Invalid URL passed to {prefix}.");
                return;
            }

            var isQueueEmpty = IsVideoQueueEmpty(roomId);
            var position = EnqueueVideoToRoom(roomId, videoUrl);
            await BroadcastCommandAsync(roomId, user.Name, prefix, videoUrl);
            await BroadcastSystemAsync(roomId, $"New video added to the queue. Current position: {position}");

            if (string.IsNullOrEmpty(room.CurrentVideoPlayingUrl) && isQueueEmpty)
            {
                //room.CurrentVideoPlayingUrl = videoUrl;
                await RunPlayCommandAsync(roomId, user, "/play");
            }
        }

        private async Task RunSkipCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is null)
                throw new ApplicationException("Room not found");

            room.CurrentVideoPlayingUrl = string.Empty;
            string[] content = [];
            string message = "The video queue is empty.";

            var nextVideo = DequeueVideoFromRoom(roomId);
            if (nextVideo is not null)
            {
                var isEmpty = IsVideoQueueEmpty(roomId);
                room.CurrentVideoPlayingUrl = nextVideo;

                content = [.. content, nextVideo.ToString()];
                message = "Current video skipped.";
                if (!isEmpty)
                    message += $" Now playing: {nextVideo}.";
                else
                    message += $" Queue is now empty.";
            }

            await BroadcastCommandAsync(roomId, user.Name, command, content);
            await BroadcastSystemAsync(roomId, message);
        }

        private async Task RunCurrentCommandAsync(string roomId, User user, string command)
        {
            var room = GetRoomById(roomId);
            if (room is null)
                throw new ApplicationException("Room not found");

            string[] content = [];
            string message = "The video queue is empty.";
            if (!string.IsNullOrEmpty(room.CurrentVideoPlayingUrl))
            {
                content = [.. content, room.CurrentVideoPlayingUrl];
                message = $"Now playing: {room.CurrentVideoPlayingUrl}";
            }
            await BroadcastCommandAsync(roomId, user.Name, command, content);
            await BroadcastSystemAsync(roomId, message);
        }

        private async Task RunListCommandAsync(string roomId, User user, string command)
        {
            var videos = ListAllVideosInRoom(roomId);
            await BroadcastCommandAsync(roomId, user.Name, command, [.. videos]);
            if (videos.Count == 0)
            {
                await BroadcastSystemAsync(roomId, $"The video queue is empty.");
                return;
            }

            await BroadcastSystemAsync(roomId, $"Current queue: {string.Join(",", videos)}");
        }
    }
}