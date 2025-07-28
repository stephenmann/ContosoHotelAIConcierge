using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoHotels.Data;
using ContosoHotels.Models;
using ContosoHotels.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoHotels.Controllers
{
    public class HousekeepingController : Controller
    {
        private readonly ContosoHotelsContext _context;

        public HousekeepingController(ContosoHotelsContext context)
        {
            _context = context;
        }

        // GET: Housekeeping
        public async Task<IActionResult> Index(string searchRoomNumber, DateTime? searchDateFrom, DateTime? searchDateTo, 
            bool showOnlyOpenRequests = true, int page = 1, int pageSize = 10)
        {
            // Build the query for housekeeping requests
            var query = _context.HousekeepingRequests
                .Include(h => h.Booking)
                .Include(h => h.Booking.Customer)
                .Include(h => h.Booking.Room)
                .AsQueryable();

            // Apply open requests filter if needed
            if (showOnlyOpenRequests)
            {
                query = query.Where(h => 
                    h.Status == HousekeepingRequestStatus.Requested || 
                    h.Status == HousekeepingRequestStatus.InProgress);
            }

            // Apply search filters
            if (!string.IsNullOrEmpty(searchRoomNumber))
            {
                query = query.Where(h => h.Booking.Room.RoomNumber.Contains(searchRoomNumber));
            }

            if (searchDateFrom.HasValue)
            {
                query = query.Where(h => h.RequestDate >= searchDateFrom.Value);
            }

            if (searchDateTo.HasValue)
            {
                query = query.Where(h => h.RequestDate <= searchDateTo.Value);
            }

            // Get total count for pagination
            var totalRequests = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRequests / pageSize);

            // Apply pagination and ordering
            var requests = await query
                .OrderByDescending(h => h.RequestDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HousekeepingRequestItem
                {
                    HousekeepingRequestId = h.HousekeepingRequestId,
                    BookingId = h.BookingId,
                    CustomerName = h.Booking.Customer.FirstName + " " + h.Booking.Customer.LastName,
                    RoomNumber = h.Booking.Room.RoomNumber,
                    RequestType = h.RequestType.ToString(),
                    Status = h.Status.ToString(),
                    RequestDate = h.RequestDate,
                    CompletionDate = h.CompletionDate,
                    Notes = h.Notes ?? string.Empty
                })
                .ToListAsync();

            // Create view model
            var viewModel = new HousekeepingViewModel
            {
                Requests = requests,
                ShowOnlyOpenRequests = showOnlyOpenRequests,
                SearchRoomNumber = searchRoomNumber ?? string.Empty,
                SearchDateFrom = searchDateFrom,
                SearchDateTo = searchDateTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRequests = totalRequests,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Housekeeping/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.HousekeepingRequests
                .Include(h => h.Booking)
                .Include(h => h.Booking.Customer)
                .Include(h => h.Booking.Room)
                .FirstOrDefaultAsync(m => m.HousekeepingRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // POST: Housekeeping/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var request = await _context.HousekeepingRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<HousekeepingRequestStatus>(status, out var requestStatus))
            {
                request.Status = requestStatus;
                if (requestStatus == HousekeepingRequestStatus.Completed)
                {
                    request.CompletionDate = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Request status updated to {status} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status provided.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
