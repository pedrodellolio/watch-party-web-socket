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
        string? roomId = context.Request.Query["room"]; // (ex: /ws?room=nomeDaSala)

        if (string.IsNullOrEmpty(roomId))
            roomId = "default";

        await RoomManager.Instance.HandleClientAsync(roomId, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
