using System.ComponentModel.DataAnnotations;

namespace ContosoHotels.Configuration
{
    public class AIConciergeConfiguration
    {
        public const string SectionName = "AIConcierge";

        public AzureAIConfiguration Azure { get; set; } = new AzureAIConfiguration();

        public OpenAIConfiguration OpenAI { get; set; } = new OpenAIConfiguration();

        [Required]
        public AgentsConfiguration Agents { get; set; } = new AgentsConfiguration();

        [Required]
        public ChatConfiguration Chat { get; set; } = new ChatConfiguration();

        /// <summary>
        /// Gets the preferred AI provider. Returns "Azure" if Azure is configured, otherwise "OpenAI"
        /// </summary>
        public string PreferredProvider => !string.IsNullOrEmpty(Azure.ApiKey) ? "Azure" : "OpenAI";
    }

    public class AzureAIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public string ChatDeploymentName { get; set; } = "gpt-4";

        public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";

        public string ApiVersion { get; set; } = "2024-02-01";

        [Range(1, 4000)]
        public int MaxTokens { get; set; } = 2000;

        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Indicates if Azure AI configuration is complete and valid
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(Endpoint);
    }

    public class OpenAIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;

        public string ChatModel { get; set; } = "gpt-4";

        public string EmbeddingModel { get; set; } = "text-embedding-ada-002";

        [Range(1, 4000)]
        public int MaxTokens { get; set; } = 2000;

        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Indicates if OpenAI configuration is complete and valid
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(ApiKey);
    }

    public class AgentsConfiguration
    {
        public AgentSettings RoomBooking { get; set; } = new AgentSettings();
        public AgentSettings RoomService { get; set; } = new AgentSettings();
        public AgentSettings Housekeeping { get; set; } = new AgentSettings();
    }

    public class AgentSettings
    {
        public bool Enabled { get; set; } = true;

        [Range(1, 50)]
        public int MaxConversationTurns { get; set; } = 20;
    }

    public class ChatConfiguration
    {
        [Range(1, 5000)]
        public int MaxMessageLength { get; set; } = 1000;

        [Range(1, 1440)]
        public int SessionTimeoutMinutes { get; set; } = 30;

        public bool EnableFileUploads { get; set; } = false;

        [Range(1, 100)]
        public int RateLimitMessagesPerMinute { get; set; } = 20;
    }
}
