using Microsoft.AspNetCore.SignalR;

namespace MesServer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }
        // Отправка сообщения всем клиентам
        public async Task SendMessage(string user, string message)
        {
            _logger.LogInformation("ChatHub.SendMessage Message received from {User}: {Message}", user, message);
            try
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
                _logger.LogDebug("ChatHub.SendMessage Message broadcasted to all clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatHub.SendMessage Error sending message from {User}", user);
                throw;
            }
        }
        // Отправка приватного сообщения конкретному пользователю
        public async Task SendPrivateMessage(string recipientId, string messageText)
        {
            var senderId = Context.UserIdentifier;
            await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", new
            {
                SenderId = senderId,
                Text = messageText,
                SentTime = DateTime.UtcNow
            });
        }
        // Обработчик подключения клиента
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Обработчик отключения клиента
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("ChatHub.SendMessage Client disconnected: {ConnectionId}", Context.ConnectionId);
            if (exception != null)
            {
                _logger.LogError(exception, "ChatHub.SendMessage Client disconnected with error");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
