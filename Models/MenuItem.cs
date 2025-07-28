using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoHotels.Models
{
    /// <summary>
    /// Menu items available for room service orders
    /// </summary>
    public class MenuItem
    {
        [Key]
        public int MenuItemId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // Appetizer, Main Course, Dessert, Beverage, etc.

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Whether the item is currently available
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Dietary restrictions and allergen information
        /// </summary>
        [StringLength(500)]
        public string DietaryInfo { get; set; } = string.Empty; // "Vegetarian, Gluten-Free, Contains Nuts"

        /// <summary>
        /// Preparation time in minutes
        /// </summary>
        public int PreparationTimeMinutes { get; set; } = 30;

        /// <summary>
        /// Image URL or filename for the menu item
        /// </summary>
        [StringLength(255)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Popularity score for recommendations (0-100)
        /// </summary>
        [Range(0, 100)]
        public int PopularityScore { get; set; } = 50;

        /// <summary>
        /// Whether this item can be customized
        /// </summary>
        public bool IsCustomizable { get; set; } = false;

        /// <summary>
        /// Spice level (0=None, 1=Mild, 2=Medium, 3=Hot, 4=Very Hot)
        /// </summary>
        [Range(0, 4)]
        public int SpiceLevel { get; set; } = 0;

        /// <summary>
        /// Whether the item is available for 24/7 service
        /// </summary>
        public bool Available24Hours { get; set; } = false;
    }
}
