using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoHotels.Models
{
    /// <summary>
    /// Represents a single message in a conversation
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public Guid ConversationId { get; set; }

        /// <summary>
        /// True if message is from user, false if from AI agent
        /// </summary>
        [Required]
        public bool IsFromUser { get; set; }

        /// <summary>
        /// The actual message content
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string MessageText { get; set; } = string.Empty;

        /// <summary>
        /// Which agent handled this message (RoomBooking, RoomService, Housekeeping)
        /// </summary>
        [StringLength(50)]
        public string AgentType { get; set; } = string.Empty;

        /// <summary>
        /// When the message was sent/received
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata stored as JSON (attachments, intent scores, etc.)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string MessageMetadata { get; set; } = string.Empty;

        /// <summary>
        /// Message sequence number within the conversation
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Whether this message contains sensitive information (for filtering)
        /// </summary>
        public bool ContainsSensitiveData { get; set; } = false;

        /// <summary>
        /// Navigation property
        /// </summary>
        [ForeignKey("ConversationId")]
        public virtual ConversationHistory Conversation { get; set; }
    }
}
