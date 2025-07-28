using System;
using System.Collections.Generic;
using ContosoHotels.Models;

namespace ContosoHotels.ViewModels
{
    public class ManagerViewModel
    {
        public IEnumerable<BookingHistoryItem> Bookings { get; set; } = new List<BookingHistoryItem>();
        public string SearchCustomerName { get; set; } = string.Empty;
        public DateTime? SearchDateFrom { get; set; }
        public DateTime? SearchDateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalBookings { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class BookingHistoryItem
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public int NumberOfNights { get; set; }
    }
}
