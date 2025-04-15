using Npgsql;
using System.Data;
using System.Net.WebSockets;
using WatchParty.WS.Managers;
using WatchParty.WS.Repositories.UserRepository;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        string? roomId = context.Request.Query["room"];
        string? userId = context.Request.Query["userId"];
        if (!string.IsNullOrEmpty(roomId) && !string.IsNullOrEmpty(userId))
        {
            using var scope = app.Services.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user is null)
            {
                context.Response.StatusCode = 404;
                return;
            }
            user.WebSocket = webSocket;
            await RoomManager.Instance.HandleClientAsync(roomId, user);
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
