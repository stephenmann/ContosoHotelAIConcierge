using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ContosoHotels.Configuration;
using ContosoHotels.Data;
using ContosoHotels.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace ContosoHotels.Services
{
    /// <summary>
    /// Main AI Concierge Service that orchestrates conversations using Semantic Kernel
    /// Routes user messages to appropriate specialized agents and manages conversation flow
    /// </summary>
    public class AIConciergeService
    {
        private readonly Kernel _kernel;
        private readonly ContosoHotelsContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<AIConciergeService> _logger;
        private readonly AIConciergeConfiguration _config;

        // Intent classification system
        private readonly Dictionary<string, string> _intentPatterns = new Dictionary<string, string>
        {
            // Booking-related intents
            { "booking", "book|reservation|reserve|stay|room|check.?in|check.?out|available|vacancy" },
            
            // Room service intents
            { "service", "food|menu|order|dining|meal|breakfast|lunch|dinner|drink|beverage|hungry|eat" },
            
            // Housekeeping intents
            { "housekeeping", "clean|housekeeping|towel|linen|maintenance|repair|broken|fix|toilet|shower" },
            
            // General inquiries
            { "general", "help|info|information|amenities|facilities|pool|gym|spa|wifi|parking|location|address" }
        };

        public AIConciergeService(
            Kernel kernel,
            ContosoHotelsContext context,
            IHubContext<ChatHub> hubContext,
            ILogger<AIConciergeService> logger,
            IOptions<AIConciergeConfiguration> config)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Process incoming user message and route to appropriate agent
        /// </summary>
        public async Task<AgentResponse> ProcessMessageAsync(
            Guid conversationId, 
            string userMessage, 
            string userId = null)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Processing message for conversation {ConversationId}: {Message}", 
                    conversationId, userMessage);

                // Get conversation context
                var context = await GetConversationContextAsync(conversationId);
                
                // Classify user intent
                var intent = await ClassifyIntentAsync(userMessage, context);
                
                // Determine appropriate agent
                var agentType = DetermineAgentType(intent, context);
                
                // Generate AI response using Semantic Kernel
                var response = await GenerateResponseAsync(userMessage, agentType, context);
                
                // Log agent interaction
                await LogAgentInteractionAsync(conversationId, agentType, "ProcessMessage", 
                    true, DateTime.UtcNow - startTime, null, userMessage);
                
                // Update conversation context
                await UpdateConversationContextAsync(conversationId, agentType, userMessage, response);
                
                return new AgentResponse
                {
                    AgentType = agentType,
                    Message = response,
                    Intent = intent,
                    Confidence = CalculateConfidence(intent, userMessage),
                    ConversationId = conversationId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for conversation {ConversationId}", conversationId);
                
                // Log failed interaction
                await LogAgentInteractionAsync(conversationId, "general", "ProcessMessage", 
                    false, DateTime.UtcNow - startTime, ex.Message, userMessage);
                
                return new AgentResponse
                {
                    AgentType = "general",
                    Message = "I apologize, but I'm experiencing technical difficulties right now. Please try again in a moment, or contact our front desk for immediate assistance.",
                    Intent = "error",
                    Confidence = 1.0f,
                    ConversationId = conversationId,
                    Timestamp = DateTime.UtcNow,
                    HasError = true,
                    ErrorMessage = "Service temporarily unavailable"
                };
            }
        }

        /// <summary>
        /// Classify user intent using pattern matching and Semantic Kernel
        /// </summary>
        private async Task<string> ClassifyIntentAsync(string message, ConversationContext context)
        {
            try
            {
                var lowerMessage = message.ToLower();
                
                // First, try pattern-based classification for speed
                foreach (var pattern in _intentPatterns)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(lowerMessage, pattern.Value))
                    {
                        _logger.LogDebug("Intent classified as {Intent} using pattern matching", pattern.Key);
                        return pattern.Key;
                    }
                }

                // If available, use Semantic Kernel for more sophisticated intent classification
                if (_kernel != null)
                {
                    var intentPrompt = $@"
                        Classify the following hotel guest message into one of these categories:
                        - booking: room reservations, availability, check-in/out
                        - service: room service, food, dining, menu requests
                        - housekeeping: cleaning, maintenance, towels, linens
                        - general: amenities, information, help

                        Previous context: {(context?.LastAgentType ?? "none")}
                        Message: {message}

                        Respond with only the category name (booking, service, housekeeping, or general).
                    ";

                    var result = await _kernel.InvokePromptAsync(intentPrompt);
                    var intent = result.GetValue<string>()?.Trim().ToLower();
                    
                    if (_intentPatterns.ContainsKey(intent))
                    {
                        _logger.LogDebug("Intent classified as {Intent} using Semantic Kernel", intent);
                        return intent;
                    }
                }

                // Default fallback
                _logger.LogDebug("Intent defaulted to 'general' - no clear classification");
                return "general";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in intent classification, defaulting to 'general'");
                return "general";
            }
        }

        /// <summary>
        /// Generate AI response using Semantic Kernel
        /// </summary>
        private async Task<string> GenerateResponseAsync(string userMessage, string agentType, ConversationContext context)
        {
            try
            {
                if (_kernel == null)
                {
                    // Fallback to simple responses if Semantic Kernel is not available
                    return GetFallbackResponse(agentType, userMessage);
                }

                var systemPrompt = GetSystemPrompt(agentType);
                var contextInfo = BuildContextInfo(context);
                
                var prompt = $@"
                    {systemPrompt}

                    Context: {contextInfo}
                    
                    Guest message: {userMessage}
                    
                    Provide a helpful, professional response as a Contoso Hotels {GetAgentTitle(agentType)}. 
                    Keep responses concise but informative. Use a warm, hospitality-focused tone.
                ";

                var result = await _kernel.InvokePromptAsync(prompt);
                var response = result.GetValue<string>()?.Trim();

                if (string.IsNullOrEmpty(response))
                {
                    return GetFallbackResponse(agentType, userMessage);
                }

                _logger.LogDebug("Generated AI response for {AgentType}: {Response}", agentType, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating AI response, using fallback");
                return GetFallbackResponse(agentType, userMessage);
            }
        }

        /// <summary>
        /// Get conversation context from database
        /// </summary>
        private async Task<ConversationContext> GetConversationContextAsync(Guid conversationId)
        {
            try
            {
                var conversation = await _context.ConversationHistories
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                {
                    return new ConversationContext { ConversationId = conversationId };
                }

                // Get recent messages for context
                var recentMessages = await _context.ChatMessages
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(5)
                    .ToListAsync();

                // Get recent agent interactions
                var recentInteractions = await _context.AgentInteractions
                    .Where(i => i.ConversationId == conversationId)
                    .OrderByDescending(i => i.Timestamp)
                    .Take(3)
                    .ToListAsync();

                return new ConversationContext
                {
                    ConversationId = conversationId,
                    SessionId = conversation.SessionId,
                    UserId = conversation.UserId,
                    StartTime = conversation.StartTime,
                    LastAgentType = recentInteractions.FirstOrDefault()?.AgentType,
                    RecentMessages = recentMessages,
                    RecentInteractions = recentInteractions,
                    MessageCount = recentMessages.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting conversation context for {ConversationId}", conversationId);
                return new ConversationContext { ConversationId = conversationId };
            }
        }

        /// <summary>
        /// Update conversation context after processing
        /// </summary>
        private async Task UpdateConversationContextAsync(
            Guid conversationId, 
            string agentType, 
            string userMessage, 
            string agentResponse)
        {
            try
            {
                // This could be extended to store conversation state, user preferences, etc.
                // For now, the database interactions handle most context storage
                _logger.LogDebug("Conversation context updated for {ConversationId} with agent {AgentType}", 
                    conversationId, agentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating conversation context");
            }
        }

        /// <summary>
        /// Log agent interaction for analytics
        /// </summary>
        private async Task LogAgentInteractionAsync(
            Guid conversationId,
            string agentType,
            string action,
            bool success,
            TimeSpan duration,
            string errorMessage = null,
            string actionContext = null)
        {
            try
            {
                var interaction = new AgentInteraction
                {
                    ConversationId = conversationId,
                    AgentType = agentType,
                    Action = action,
                    Success = success,
                    Duration = (int)duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    ErrorMessage = errorMessage,
                    ActionContext = actionContext,
                    IntentConfidence = CalculateConfidence(agentType, actionContext ?? "")
                };

                _context.AgentInteractions.Add(interaction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error logging agent interaction");
            }
        }

        /// <summary>
        /// Determine which agent should handle the message
        /// </summary>
        private string DetermineAgentType(string intent, ConversationContext context)
        {
            // Consider conversation context for agent continuity
            if (context?.LastAgentType != null && ShouldContinueWithAgent(intent, context.LastAgentType))
            {
                return context.LastAgentType;
            }

            // Route based on intent
            return intent switch
            {
                "booking" => "booking",
                "service" => "service", 
                "housekeeping" => "housekeeping",
                _ => "general"
            };
        }

        /// <summary>
        /// Check if conversation should continue with the same agent
        /// </summary>
        private bool ShouldContinueWithAgent(string newIntent, string lastAgentType)
        {
            // Continue with same agent for follow-up questions
            if (newIntent == "general" && lastAgentType != "general")
            {
                return true;
            }

            // Continue if intents match
            return newIntent == lastAgentType;
        }

        /// <summary>
        /// Get system prompt for specific agent type
        /// </summary>
        private string GetSystemPrompt(string agentType)
        {
            return agentType switch
            {
                "booking" => @"You are a professional hotel booking specialist at Contoso Hotels. 
                    Help guests with room reservations, availability, pricing, and booking-related inquiries. 
                    You have access to room inventory and can check availability.",

                "service" => @"You are a room service coordinator at Contoso Hotels. 
                    Help guests with food orders, menu questions, dietary accommodations, and dining services. 
                    Our kitchen operates 24/7 with full menu availability.",

                "housekeeping" => @"You are a housekeeping coordinator at Contoso Hotels. 
                    Help guests with cleaning requests, maintenance issues, amenity requests, and room services. 
                    You can schedule services and handle urgent requests.",

                _ => @"You are a helpful concierge at Contoso Hotels. 
                    Provide information about hotel amenities, local attractions, and general assistance. 
                    Always be professional and helpful."
            };
        }

        /// <summary>
        /// Get agent title for responses
        /// </summary>
        private string GetAgentTitle(string agentType)
        {
            return agentType switch
            {
                "booking" => "Booking Specialist",
                "service" => "Room Service Coordinator", 
                "housekeeping" => "Housekeeping Coordinator",
                _ => "Concierge"
            };
        }

        /// <summary>
        /// Build context information for AI prompt
        /// </summary>
        private string BuildContextInfo(ConversationContext context)
        {
            if (context == null || context.RecentMessages?.Any() != true)
            {
                return "This is the start of a new conversation.";
            }

            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine($"Conversation started: {context.StartTime:yyyy-MM-dd HH:mm}");
            
            if (!string.IsNullOrEmpty(context.LastAgentType))
            {
                contextBuilder.AppendLine($"Previous agent: {context.LastAgentType}");
            }

            if (context.RecentMessages.Count > 1)
            {
                contextBuilder.AppendLine("Recent conversation:");
                foreach (var msg in context.RecentMessages.OrderBy(m => m.Timestamp).TakeLast(3))
                {
                    var sender = msg.IsFromUser ? "Guest" : $"Agent ({msg.AgentType})";
                    contextBuilder.AppendLine($"{sender}: {msg.MessageText}");
                }
            }

            return contextBuilder.ToString();
        }

        /// <summary>
        /// Calculate confidence score for intent classification
        /// </summary>
        private float CalculateConfidence(string intent, string message)
        {
            if (string.IsNullOrEmpty(message) || !_intentPatterns.ContainsKey(intent))
            {
                return 0.5f;
            }

            var pattern = _intentPatterns[intent];
            var matches = System.Text.RegularExpressions.Regex.Matches(message.ToLower(), pattern);
            
            // Simple confidence calculation based on pattern matches
            var confidence = Math.Min(0.5f + (matches.Count * 0.2f), 1.0f);
            return confidence;
        }

        /// <summary>
        /// Get fallback response when Semantic Kernel is unavailable
        /// </summary>
        private string GetFallbackResponse(string agentType, string message)
        {
            return agentType switch
            {
                "booking" => "I'd be happy to help you with your room booking needs! Could you please tell me your preferred dates and the number of guests?",
                
                "service" => "I can help you with room service! Our kitchen is open 24/7. Would you like to see our menu or do you have something specific in mind?",
                
                "housekeeping" => "I can assist you with housekeeping services. What type of service do you need - cleaning, fresh linens, maintenance, or something else?",
                
                _ => "Thank you for contacting Contoso Hotels! I'm here to help with any questions about our services, amenities, or your stay. How can I assist you today?"
            };
        }
    }

    /// <summary>
    /// Response from AI agent processing
    /// </summary>
    public class AgentResponse
    {
        public string AgentType { get; set; }
        public string Message { get; set; }
        public string Intent { get; set; }
        public float Confidence { get; set; }
        public Guid ConversationId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Conversation context for agent decision making
    /// </summary>
    public class ConversationContext
    {
        public Guid ConversationId { get; set; }
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime StartTime { get; set; }
        public string LastAgentType { get; set; }
        public List<ChatMessage> RecentMessages { get; set; } = new List<ChatMessage>();
        public List<AgentInteraction> RecentInteractions { get; set; } = new List<AgentInteraction>();
        public int MessageCount { get; set; }
        public Dictionary<string, object> UserPreferences { get; set; } = new Dictionary<string, object>();
    }
}
