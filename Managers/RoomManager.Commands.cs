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
                    await RunPlayCommandAsync(roomId, user.Name);
                    break;
                case "/pause":
                    await RunPauseCommandAsync(roomId, user.Name);
                    break;
                case "/queue":
                    await RunQueueCommandAsync(roomId, user.WebSocket, command);
                    break;
                case "/skip":
                    await RunSkipCommandAsync(roomId, user.WebSocket);
                    break;
                case "/current":
                    await RunCurrentCommandAsync(roomId, user.WebSocket);
                    break;
                case "/list":
                    await RunListCommandAsync(roomId, user.WebSocket);
                    break;
            }
        }

        private async Task RunPlayCommandAsync(string roomId, string username)
        {
            var room = GetRoomById(roomId);
            if (room is not null)
                room.IsVideoPlaying = true;

            var videoUrl = PeekNextVideoInRoom(roomId);
            if (string.IsNullOrEmpty(videoUrl))
                return;
            await BroadcastCommandAsync(roomId, username, "/play", videoUrl);
            await BroadcastSystemAsync(roomId, $"{username} started the video.");
        }

        private async Task RunPauseCommandAsync(string roomId, string username)
        {
            var room = GetRoomById(roomId);
            if (room is not null)
                room.IsVideoPlaying = false;
            await BroadcastSystemAsync(roomId, $"{username} paused the video.");
        }

        private async Task RunQueueCommandAsync(string roomId, WebSocket webSocket, string command)
        {
            var prefix = command.Split(' ')[0];
            var commandParams = command.GetCommandParams();
            if (commandParams.Length == 0)
                await SendDirectSystemAsync(webSocket, $"Invalid parameters passed to {prefix}.");
            else
            {
                var videoUrl = commandParams[0].TryConvertToYoutubeUrl();
                if (string.IsNullOrEmpty(videoUrl))
                {
                    await SendDirectSystemAsync(webSocket, $"Invalid URL passed to {prefix}.");
                    return;
                }

                var position = EnqueueVideoToRoom(roomId, videoUrl);
                await BroadcastSystemAsync(roomId, $"New video added to the queue. Current position: {position}");

                //var room = GetRoomById(roomId);
                //if (room is not null && !room.IsVideoPlaying)
                //{
                //    await BroadcastActionAsync(roomId, "NOW_PLAYING", videoUrl);
                //    room.IsVideoPlaying = true;
                //}
            }
        }

        private async Task RunSkipCommandAsync(string roomId, WebSocket webSocket)
        {
            DequeueVideoFromRoom(roomId);
            var nextVideo = PeekNextVideoInRoom(roomId);
            if (nextVideo is not null)
            {
                await BroadcastVideoAsync(roomId, nextVideo);
                await BroadcastSystemAsync(roomId, $"Current video skipped. Now playing: {nextVideo}");
            }
            else
                await SendDirectSystemAsync(webSocket, "The video queue is empty.");
        }

        private async Task RunCurrentCommandAsync(string roomId, WebSocket webSocket)
        {
            var currentVideo = PeekNextVideoInRoom(roomId);
            if (currentVideo is not null)
                await SendDirectSystemAsync(webSocket, $"Now playing: {currentVideo}");
            else
                await SendDirectSystemAsync(webSocket, "The video queue is empty.");
        }

        private async Task RunListCommandAsync(string roomId, WebSocket webSocket)
        {
            var videos = ListAllVideosInRoom(roomId);
            await BroadcastVideoQueueAsync(roomId, webSocket, videos);
        }
    }
}