using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using ContosoHotels.Data;
using ContosoHotels.Models;
using ContosoHotels.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ContosoHotels.Services.Agents
{
    /// <summary>
    /// Base class for all AI agents in the Contoso Hotels system
    /// Provides common functionality for conversation handling and response generation
    /// </summary>
    public abstract class BaseAgent
    {
        protected readonly Kernel _kernel;
        protected readonly ContosoHotelsContext _context;
        protected readonly ILogger _logger;

        public abstract string AgentType { get; }
        public abstract string AgentName { get; }
        public abstract string AgentDescription { get; }

        protected BaseAgent(
            Kernel kernel,
            ContosoHotelsContext context,
            ILogger logger)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Process a message specific to this agent type
        /// </summary>
        public abstract Task<AgentResponse> ProcessMessageAsync(
            string message, 
            ConversationContext context);

        /// <summary>
        /// Check if this agent can handle the given intent
        /// </summary>
        public abstract bool CanHandle(string intent, ConversationContext context);

        /// <summary>
        /// Get the priority of this agent for handling a specific intent
        /// Higher numbers indicate higher priority
        /// </summary>
        public abstract int GetPriority(string intent, ConversationContext context);

        /// <summary>
        /// Generate a response using Semantic Kernel
        /// </summary>
        protected async Task<string> GenerateResponseAsync(
            string userMessage, 
            string systemPrompt, 
            ConversationContext context)
        {
            try
            {
                if (_kernel == null)
                {
                    return GetFallbackResponse(userMessage);
                }

                var contextInfo = BuildContextInfo(context);
                
                var prompt = $@"
                    {systemPrompt}

                    Context: {contextInfo}
                    
                    Guest message: {userMessage}
                    
                    Provide a helpful, professional response as a Contoso Hotels {AgentName}. 
                    Keep responses concise but informative. Use a warm, hospitality-focused tone.
                    If you need additional information to help the guest, ask specific questions.
                ";

                var result = await _kernel.InvokePromptAsync(prompt);
                var response = result.GetValue<string>()?.Trim();

                if (string.IsNullOrEmpty(response))
                {
                    return GetFallbackResponse(userMessage);
                }

                _logger.LogDebug("Generated AI response for {AgentType}: {Response}", AgentType, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating AI response for {AgentType}, using fallback", AgentType);
                return GetFallbackResponse(userMessage);
            }
        }

        /// <summary>
        /// Build context information for AI prompt
        /// </summary>
        protected virtual string BuildContextInfo(ConversationContext context)
        {
            if (context == null)
            {
                return "This is the start of a new conversation.";
            }

            var contextParts = new List<string>();

            if (context.StartTime != default)
            {
                contextParts.Add($"Conversation started: {context.StartTime:HH:mm}");
            }

            if (!string.IsNullOrEmpty(context.LastAgentType) && context.LastAgentType != AgentType)
            {
                contextParts.Add($"Previous agent: {context.LastAgentType}");
            }

            if (context.RecentMessages?.Count > 1)
            {
                contextParts.Add("Recent conversation summary available");
            }

            if (context.UserPreferences?.Count > 0)
            {
                contextParts.Add("User preferences available");
            }

            return contextParts.Count > 0 
                ? string.Join(". ", contextParts) 
                : "New conversation";
        }

        /// <summary>
        /// Get fallback response when AI is unavailable
        /// </summary>
        protected abstract string GetFallbackResponse(string userMessage);

        /// <summary>
        /// Validate and sanitize user input
        /// </summary>
        protected virtual string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Basic sanitization - remove excessive whitespace
            input = input.Trim();
            
            // Remove or replace potentially problematic characters
            input = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ");
            
            return input;
        }

        /// <summary>
        /// Create a successful agent response
        /// </summary>
        protected AgentResponse CreateSuccessResponse(
            string message, 
            string intent, 
            float confidence,
            Guid conversationId,
            Dictionary<string, object> metadata = null)
        {
            return new AgentResponse
            {
                AgentType = AgentType,
                Message = message,
                Intent = intent,
                Confidence = confidence,
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow,
                HasError = false,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Create an error agent response
        /// </summary>
        protected AgentResponse CreateErrorResponse(
            string errorMessage, 
            Guid conversationId,
            string fallbackMessage = null)
        {
            return new AgentResponse
            {
                AgentType = AgentType,
                Message = fallbackMessage ?? GetFallbackResponse("error"),
                Intent = "error",
                Confidence = 1.0f,
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow,
                HasError = true,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Log agent activity for analytics
        /// </summary>
        protected async Task LogAgentActivityAsync(
            Guid conversationId,
            string action,
            bool success,
            TimeSpan duration,
            string context = null,
            string errorMessage = null)
        {
            try
            {
                var interaction = new AgentInteraction
                {
                    ConversationId = conversationId,
                    AgentType = AgentType,
                    Action = action,
                    Success = success,
                    Duration = (int)duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    ErrorMessage = errorMessage,
                    ActionContext = context,
                    IntentConfidence = success ? 0.8f : 0.1f
                };

                _context.AgentInteractions.Add(interaction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log agent activity for {AgentType}", AgentType);
            }
        }

        /// <summary>
        /// Check if message contains sensitive information
        /// </summary>
        protected virtual bool ContainsSensitiveData(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var lowerMessage = message.ToLower();
            
            // Basic patterns for sensitive data
            var sensitivePatterns = new[]
            {
                @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", // Credit card numbers
                @"\b\d{3}-\d{2}-\d{4}\b", // SSN
                @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", // Phone numbers
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Email addresses
            };

            foreach (var pattern in sensitivePatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(message, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extract entities from user message (dates, numbers, locations, etc.)
        /// </summary>
        protected virtual Dictionary<string, object> ExtractEntities(string message)
        {
            var entities = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(message))
                return entities;

            // Extract numbers
            var numbers = System.Text.RegularExpressions.Regex.Matches(message, @"\b\d+\b");
            if (numbers.Count > 0)
            {
                entities["numbers"] = numbers.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => int.Parse(m.Value)).ToArray();
            }

            // Extract dates (basic patterns)
            var datePatterns = new[]
            {
                @"\b\d{1,2}[-/]\d{1,2}[-/]\d{4}\b",
                @"\b\d{4}[-/]\d{1,2}[-/]\d{1,2}\b",
                @"\b(today|tomorrow|yesterday)\b",
                @"\b(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b"
            };

            var dates = new List<string>();
            foreach (var pattern in datePatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(message.ToLower(), pattern);
                dates.AddRange(matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value));
            }

            if (dates.Any())
            {
                entities["dates"] = dates.ToArray();
            }

            return entities;
        }
    }
}
