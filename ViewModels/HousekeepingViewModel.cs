using System;
using System.Collections.Generic;
using ContosoHotels.Models;

namespace ContosoHotels.ViewModels
{
    public class HousekeepingViewModel
    {
        public IEnumerable<HousekeepingRequestItem> Requests { get; set; } = new List<HousekeepingRequestItem>();
        public bool ShowOnlyOpenRequests { get; set; } = true;
        public string SearchRoomNumber { get; set; } = string.Empty;
        public DateTime? SearchDateFrom { get; set; }
        public DateTime? SearchDateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRequests { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class HousekeepingRequestItem
    {
        public int HousekeepingRequestId { get; set; }
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool IsOpen => Status == HousekeepingRequestStatus.Requested.ToString() || 
                            Status == HousekeepingRequestStatus.InProgress.ToString();
    }
}
