using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ContosoHotels.Data;
using ContosoHotels.Models;
using ContosoHotels.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ContosoHotels.Controllers
{
    /// <summary>
    /// API Controller for chat-related HTTP operations
    /// Handles conversation creation, history retrieval, and chat metadata
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ContosoHotelsContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ContosoHotelsContext context,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Create a new conversation session
        /// </summary>
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            try
            {
                var conversation = new ConversationHistory
                {
                    ConversationId = Guid.NewGuid(),
                    SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ConversationHistories.Add(conversation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new conversation {ConversationId} for session {SessionId}", 
                    conversation.ConversationId, conversation.SessionId);

                return Ok(new ConversationResponse
                {
                    ConversationId = conversation.ConversationId,
                    SessionId = conversation.SessionId,
                    StartTime = conversation.StartTime,
                    IsActive = conversation.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create conversation");
                return StatusCode(500, new { Error = "Failed to create conversation" });
            }
        }

        /// <summary>
        /// Get conversation history
        /// </summary>
        [HttpGet("conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> GetConversationHistory(Guid conversationId, [FromQuery] int? limit = 50)
        {
            try
            {
                var conversation = await _context.ConversationHistories
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { Error = "Conversation not found" });
                }

                var messages = await _context.ChatMessages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.SequenceNumber)
                    .Take(limit ?? 50)
                    .Select(m => new MessageResponse
                    {
                        MessageId = m.MessageId,
                        ConversationId = m.ConversationId,
                        IsFromUser = m.IsFromUser,
                        MessageText = m.MessageText,
                        AgentType = m.AgentType,
                        Timestamp = m.Timestamp,
                        SequenceNumber = m.SequenceNumber
                    })
                    .ToListAsync();

                return Ok(new
                {
                    ConversationId = conversationId,
                    MessageCount = messages.Count,
                    Messages = messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve conversation history for {ConversationId}", conversationId);
                return StatusCode(500, new { Error = "Failed to retrieve conversation history" });
            }
        }

        /// <summary>
        /// End a conversation session
        /// </summary>
        [HttpPut("conversations/{conversationId:guid}/end")]
        public async Task<IActionResult> EndConversation(Guid conversationId)
        {
            try
            {
                var conversation = await _context.ConversationHistories
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { Error = "Conversation not found" });
                }

                conversation.EndTime = DateTime.UtcNow;
                conversation.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ended conversation {ConversationId}", conversationId);

                return Ok(new
                {
                    ConversationId = conversationId,
                    EndTime = conversation.EndTime,
                    IsActive = conversation.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end conversation {ConversationId}", conversationId);
                return StatusCode(500, new { Error = "Failed to end conversation" });
            }
        }

        /// <summary>
        /// Get active conversations (for monitoring/debugging)
        /// </summary>
        [HttpGet("conversations/active")]
        public async Task<IActionResult> GetActiveConversations([FromQuery] int? limit = 20)
        {
            try
            {
                var activeConversations = await _context.ConversationHistories
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.StartTime)
                    .Take(limit ?? 20)
                    .Select(c => new ConversationResponse
                    {
                        ConversationId = c.ConversationId,
                        SessionId = c.SessionId,
                        UserId = c.UserId,
                        StartTime = c.StartTime,
                        IsActive = c.IsActive
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Count = activeConversations.Count,
                    Conversations = activeConversations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active conversations");
                return StatusCode(500, new { Error = "Failed to retrieve active conversations" });
            }
        }

        /// <summary>
        /// Save a message to the database (called internally or via webhook)
        /// </summary>
        [HttpPost("conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> SaveMessage(Guid conversationId, [FromBody] SaveMessageRequest request)
        {
            try
            {
                // Verify conversation exists
                var conversationExists = await _context.ConversationHistories
                    .AnyAsync(c => c.ConversationId == conversationId);

                if (!conversationExists)
                {
                    return NotFound(new { Error = "Conversation not found" });
                }

                // Get next sequence number
                var lastSequence = await _context.ChatMessages
                    .Where(m => m.ConversationId == conversationId)
                    .MaxAsync(m => (int?)m.SequenceNumber) ?? 0;

                var message = new ChatMessage
                {
                    ConversationId = conversationId,
                    IsFromUser = request.IsFromUser,
                    MessageText = request.MessageText,
                    AgentType = request.AgentType,
                    Timestamp = DateTime.UtcNow,
                    SequenceNumber = lastSequence + 1,
                    MessageMetadata = request.MessageMetadata,
                    ContainsSensitiveData = request.ContainsSensitiveData
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved message {MessageId} to conversation {ConversationId}", 
                    message.MessageId, conversationId);

                return Ok(new
                {
                    MessageId = message.MessageId,
                    ConversationId = conversationId,
                    SequenceNumber = message.SequenceNumber,
                    Timestamp = message.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save message to conversation {ConversationId}", conversationId);
                return StatusCode(500, new { Error = "Failed to save message" });
            }
        }

        /// <summary>
        /// Health check endpoint for chat system
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Contoso Hotels Chat API",
                Version = "1.0.0"
            });
        }
    }

    // Request/Response DTOs
    public class CreateConversationRequest
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
    }

    public class ConversationResponse
    {
        public Guid ConversationId { get; set; }
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class MessageResponse
    {
        public int MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public bool IsFromUser { get; set; }
        public string MessageText { get; set; }
        public string AgentType { get; set; }
        public DateTime Timestamp { get; set; }
        public int SequenceNumber { get; set; }
    }

    public class SaveMessageRequest
    {
        public bool IsFromUser { get; set; }
        public string MessageText { get; set; }
        public string AgentType { get; set; }
        public string MessageMetadata { get; set; }
        public bool ContainsSensitiveData { get; set; }
    }
}
