using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoHotels.Models
{
    /// <summary>
    /// Represents a conversation session between a user and the AI concierge
    /// </summary>
    public class ConversationHistory
    {
        [Key]
        public Guid ConversationId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// User identifier (could be customer ID, session identifier, or anonymous ID)
        /// </summary>
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When the conversation started
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the conversation ended (null if still active)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Whether the conversation is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// IP address or location information for security
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User agent string for device/browser tracking
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Navigation properties
        /// </summary>
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<AgentInteraction> AgentInteractions { get; set; } = new List<AgentInteraction>();
    }
}
