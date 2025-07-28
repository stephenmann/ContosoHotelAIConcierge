using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;

namespace ContosoHotels.Services
{
    /// <summary>
    /// SignalR Hub for real-time chat communication between clients and AI concierge
    /// Handles connection management, message routing, and typing indicators
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly AIConciergeService _aiConciergeService;
        
        // Track active connections and their conversation IDs
        private static readonly ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();
        
        public ChatHub(ILogger<ChatHub> logger, AIConciergeService aiConciergeService)
        {
            _logger = logger;
            _aiConciergeService = aiConciergeService;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            
            _logger.LogInformation("Chat client connected: {ConnectionId} - {UserAgent}", connectionId, userAgent);
            
            // Send connection confirmation to client
            await Clients.Caller.SendAsync("ConnectionEstablished", new
            {
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow,
                Message = "Connected to Contoso Hotels AI Concierge"
            });
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            
            // Remove connection from tracking
            _connections.TryRemove(connectionId, out var conversationId);
            
            if (exception != null)
            {
                _logger.LogWarning("Chat client disconnected with error: {ConnectionId} - {Error}", 
                    connectionId, exception.Message);
            }
            else
            {
                _logger.LogInformation("Chat client disconnected: {ConnectionId}", connectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a conversation session
        /// </summary>
        public async Task JoinConversation(string conversationId, string userId = null)
        {
            var connectionId = Context.ConnectionId;
            
            // Track the conversation for this connection
            _connections.AddOrUpdate(connectionId, conversationId, (key, oldValue) => conversationId);
            
            // Add to SignalR group for the conversation
            await Groups.AddToGroupAsync(connectionId, $"conversation_{conversationId}");
            
            _logger.LogInformation("Client {ConnectionId} joined conversation {ConversationId}", 
                connectionId, conversationId);
            
            // Confirm to client
            await Clients.Caller.SendAsync("ConversationJoined", new
            {
                ConversationId = conversationId,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Leave a conversation session
        /// </summary>
        public async Task LeaveConversation(string conversationId)
        {
            var connectionId = Context.ConnectionId;
            
            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(connectionId, $"conversation_{conversationId}");
            
            // Remove from tracking
            _connections.TryRemove(connectionId, out _);
            
            _logger.LogInformation("Client {ConnectionId} left conversation {ConversationId}", 
                connectionId, conversationId);
            
            await Clients.Caller.SendAsync("ConversationLeft", new
            {
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Send a message from client - this will be processed by the AI Concierge Service
        /// </summary>
        public async Task SendMessage(string conversationId, string message, string messageId = null)
        {
            var connectionId = Context.ConnectionId;
            messageId = messageId ?? Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Received message from {ConnectionId} in conversation {ConversationId}: {Message}", 
                    connectionId, conversationId, message);

                // Acknowledge message received
                await Clients.Caller.SendAsync("MessageReceived", new
                {
                    MessageId = messageId,
                    ConversationId = conversationId,
                    Timestamp = DateTime.UtcNow,
                    Status = "received"
                });

                // Show typing indicator to other clients
                await Clients.Group($"conversation_{conversationId}").SendAsync("AgentTyping", new
                {
                    ConversationId = conversationId,
                    AgentType = "processing",
                    IsTyping = true,
                    Timestamp = DateTime.UtcNow
                });

                // Process message through AI Concierge Service
                var response = await _aiConciergeService.ProcessMessageAsync(
                    Guid.Parse(conversationId), 
                    message);

                // Hide typing indicator
                await Clients.Group($"conversation_{conversationId}").SendAsync("AgentTyping", new
                {
                    ConversationId = conversationId,
                    AgentType = response.AgentType,
                    IsTyping = false,
                    Timestamp = DateTime.UtcNow
                });

                // Send agent response to conversation group
                await Clients.Group($"conversation_{conversationId}").SendAsync("AgentMessage", new
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ConversationId = conversationId,
                    OriginalMessageId = messageId,
                    AgentType = response.AgentType,
                    Message = response.Message,
                    Intent = response.Intent,
                    Confidence = response.Confidence,
                    Timestamp = response.Timestamp,
                    IsFromUser = false,
                    HasError = response.HasError,
                    ErrorMessage = response.ErrorMessage
                });
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {ConnectionId}", connectionId);
                
                // Hide typing indicator on error
                await Clients.Group($"conversation_{conversationId}").SendAsync("AgentTyping", new
                {
                    ConversationId = conversationId,
                    AgentType = "general",
                    IsTyping = false,
                    Timestamp = DateTime.UtcNow
                });
                
                await Clients.Caller.SendAsync("MessageError", new
                {
                    MessageId = messageId,
                    ConversationId = conversationId,
                    Error = "Failed to process message - our AI service is temporarily unavailable",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Send typing indicator to other participants in conversation
        /// </summary>
        public async Task SendTypingIndicator(string conversationId, bool isTyping)
        {
            var connectionId = Context.ConnectionId;
            
            // Send to all other clients in the conversation (excluding sender)
            await Clients.OthersInGroup($"conversation_{conversationId}").SendAsync("UserTyping", new
            {
                ConversationId = conversationId,
                IsTyping = isTyping,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get conversation status (for debugging/monitoring)
        /// </summary>
        public async Task GetConversationStatus(string conversationId)
        {
            var connectionCount = _connections.Values.Where(c => c == conversationId).Count();
            
            await Clients.Caller.SendAsync("ConversationStatus", new
            {
                ConversationId = conversationId,
                ActiveConnections = connectionCount,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Send agent message (called from AI Concierge Service)
        /// </summary>
        public async Task SendAgentMessage(string conversationId, string message, string agentType, string originalMessageId = null)
        {
            await Clients.Group($"conversation_{conversationId}").SendAsync("AgentMessage", new
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                OriginalMessageId = originalMessageId,
                AgentType = agentType,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsFromUser = false
            });
        }

        /// <summary>
        /// Show typing indicator for agent
        /// </summary>
        public async Task ShowAgentTyping(string conversationId, string agentType)
        {
            await Clients.Group($"conversation_{conversationId}").SendAsync("AgentTyping", new
            {
                ConversationId = conversationId,
                AgentType = agentType,
                IsTyping = true,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Hide typing indicator for agent
        /// </summary>
        public async Task HideAgentTyping(string conversationId, string agentType)
        {
            await Clients.Group($"conversation_{conversationId}").SendAsync("AgentTyping", new
            {
                ConversationId = conversationId,
                AgentType = agentType,
                IsTyping = false,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
