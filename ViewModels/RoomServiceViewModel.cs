using System;
using System.Collections.Generic;
using ContosoHotels.Models;

namespace ContosoHotels.ViewModels
{
    public class RoomServiceViewModel
    {
        public IEnumerable<RoomServiceOrderItem> Orders { get; set; } = new List<RoomServiceOrderItem>();
        public bool ShowOnlyOpenOrders { get; set; } = true;
        public string SearchRoomNumber { get; set; } = string.Empty;
        public DateTime? SearchDateFrom { get; set; }
        public DateTime? SearchDateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class RoomServiceOrderItem
    {
        public int RoomServiceId { get; set; }
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal Price { get; set; }
        public string SpecialInstructions { get; set; } = string.Empty;
        public bool IsOpen => Status == RoomServiceStatus.Requested.ToString() || 
                           Status == RoomServiceStatus.InProgress.ToString();
    }
}
