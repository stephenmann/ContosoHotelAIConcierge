using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoHotels.Models
{
    /// <summary>
    /// Tracks agent interactions for analytics and monitoring
    /// </summary>
    public class AgentInteraction
    {
        [Key]
        public int InteractionId { get; set; }

        [Required]
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Which agent was involved (RoomBooking, RoomService, Housekeeping)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AgentType { get; set; } = string.Empty;

        /// <summary>
        /// What action the agent performed
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Whether the action was successful
        /// </summary>
        [Required]
        public bool Success { get; set; }

        /// <summary>
        /// How long the action took to complete (in milliseconds)
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// When the interaction occurred
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Error message if the action failed
        /// </summary>
        [StringLength(1000)]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Additional context or parameters for the action
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string ActionContext { get; set; } = string.Empty;

        /// <summary>
        /// User intent confidence score (0.0 - 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double IntentConfidence { get; set; } = 0.0;

        /// <summary>
        /// Navigation property
        /// </summary>
        [ForeignKey("ConversationId")]
        public virtual ConversationHistory Conversation { get; set; }
    }
}
