using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoHotels.Data;
using ContosoHotels.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoHotels.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ContosoHotelsContext _context;

        public ManagerController(ContosoHotelsContext context)
        {
            _context = context;
        }

        // GET: Manager
        public async Task<IActionResult> Index(string searchCustomerName, DateTime? searchDateFrom, DateTime? searchDateTo, int page = 1, int pageSize = 10)
        {
            // Build the query for booking history
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(searchCustomerName))
            {
                var searchTerm = searchCustomerName.ToLower();
                query = query.Where(b => 
                    (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(searchTerm) ||
                    b.Customer.FirstName.ToLower().Contains(searchTerm) ||
                    b.Customer.LastName.ToLower().Contains(searchTerm));
            }

            if (searchDateFrom.HasValue)
            {
                query = query.Where(b => b.CheckInDate >= searchDateFrom.Value);
            }

            if (searchDateTo.HasValue)
            {
                query = query.Where(b => b.CheckOutDate <= searchDateTo.Value);
            }

            // Get total count for pagination
            var totalBookings = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalBookings / pageSize);

            // Apply pagination and ordering
            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingHistoryItem
                {
                    BookingId = b.BookingId,
                    CustomerName = b.Customer.FirstName + " " + b.Customer.LastName,
                    CustomerEmail = b.Customer.Email,
                    CustomerPhone = b.Customer.PhoneNumber ?? "N/A",
                    RoomType = b.Room.RoomType,
                    RoomNumber = b.Room.RoomNumber,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    TotalAmount = b.TotalAmount,
                    BookingDate = b.BookingDate,
                    Status = b.Status.ToString(),
                    NumberOfGuests = b.NumberOfGuests,
                    NumberOfNights = (b.CheckOutDate - b.CheckInDate).Days
                })
                .ToListAsync();

            // Create view model
            var viewModel = new ManagerViewModel
            {
                Bookings = bookings,
                SearchCustomerName = searchCustomerName ?? string.Empty,
                SearchDateFrom = searchDateFrom,
                SearchDateTo = searchDateTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalBookings = totalBookings,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Manager/BookingDetails/5
        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Manager/UpdateBookingStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<Models.BookingStatus>(status, out var bookingStatus))
            {
                booking.Status = bookingStatus;
                if (bookingStatus == Models.BookingStatus.Cancelled)
                {
                    booking.CancellationDate = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Booking status updated to {status} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status provided.";
            }

            return RedirectToAction(nameof(BookingDetails), new { id = id });
        }
    }
}
