using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MesServer.Data;
using MesServer.Hubs;
using MesServer.Models;
//using MesServer.Repo;
using MesServer.Services;
using System.Text;
using System.Security.Cryptography.X509Certificates;
//using MesServer.Services.Interfaces;

// Настройка и запуск веб-приложения
var builder = WebApplication.CreateBuilder(args);
var thumbprint = "E7097A86E07F50D106B40881A90A17CE73D6514E";//CN=black05 - OK!!!
//var thumbprint = "2166F28F3B665E16E6CE3CF0C681921B5A6E4493"; //CN=black05.rainbow.ru
using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
store.Open(OpenFlags.ReadOnly);
var cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];

// Очистка существующих провайдеров логирования и добавление новых
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "[HH:mm:ss] ";
});
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Настройка Kestrel сервера
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    // Прослушивание на всех IP-адресах порта 5278 (HTTP)
//    //serverOptions.ListenAnyIP(5278); // Только HTTP
//        serverOptions.ListenAnyIP(7205); // HTTP
//    serverOptions.ListenAnyIP(443, listenOptions =>
//    {
//        listenOptions.UseHttps(); // HTTPS
//    });
//});
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = cert;
    });
    options.Listen(System.Net.IPAddress.Any, 5278); // HTTP
    options.Listen(System.Net.IPAddress.Any, 7205, listenOptions =>
    {
        listenOptions.UseHttps(cert); // HTTPS с вашим сертификатом
    });
});
// Add services to the container.

// Добавление сервисов в контейнер DI
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// Настройка Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Настройка контекста базы данных
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
              //.AllowCredentials();// Если используются куки или SignalR
    });
});

// Настройка аутентификации JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        // Для SignalR
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Возвращаем 401 вместо перенаправления
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
        };
    });
var app = builder.Build();

// Настройка конвейера HTTP-запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Маршрутизация
app.MapGet("/", () => "Messenger API is running");
app.MapGet("/api/ping", () => "Pong");
app.MapHub<ChatHub>("/chatHub");

// Middleware для логирования входящих запросов
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
});
//app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
