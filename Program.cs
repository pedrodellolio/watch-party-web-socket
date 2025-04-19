using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Data;
using System.Net.WebSockets;
using System.Text;
using WatchParty.WS.Managers;
using WatchParty.WS.Repositories.UserRepository;
using WatchParty.WS.Services;

var builder = WebApplication.CreateBuilder(args);

var bytes = Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtKey")!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration.GetValue<string>("Issuer"),
        ValidAudience = builder.Configuration.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(bytes),
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/ws") &&
                context.Request.Query.TryGetValue("token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<IYoutubeService, YoutubeService>();
builder.Services.AddSingleton<RoomManager>();

var app = builder.Build();
app.UseAuthentication();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        string? roomId = context.Request.Query["room"];
        string? userId = context.Request.Query["userId"];
        if (!string.IsNullOrEmpty(roomId) && !string.IsNullOrEmpty(userId))
        {
            using var scope = app.Services.CreateScope();
            var roomManager = context.RequestServices.GetRequiredService<RoomManager>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByIdAsync(Guid.Parse(userId));

            if (user is null)
            {
                context.Response.StatusCode = 404;
                return;
            }
            user.WebSocket = webSocket;
            await roomManager.HandleClientAsync(roomId, user);
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
