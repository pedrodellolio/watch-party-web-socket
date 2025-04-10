using System.Net.WebSockets;
using WatchParty.WS;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        string? roomId = context.Request.Query["room"];
        string? username = context.Request.Query["username"];
        string? userId = context.Request.Query["userId"];

        //if (string.IsNullOrEmpty(roomId))
        //    roomId = "default";

        //if (string.IsNullOrEmpty(username))
        //    username = "default";

        //if (string.IsNullOrEmpty(userId))
        //    userId = Guid.Empty.ToString();
        if (!string.IsNullOrEmpty(roomId) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(userId))
            await RoomManager.Instance.HandleClientAsync(roomId, username, Guid.Parse(userId), webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
