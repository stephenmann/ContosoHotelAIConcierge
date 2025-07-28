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
    public class RoomServiceController : Controller
    {
        private readonly ContosoHotelsContext _context;

        public RoomServiceController(ContosoHotelsContext context)
        {
            _context = context;
        }

        // GET: RoomService
        public async Task<IActionResult> Index(string searchRoomNumber, DateTime? searchDateFrom, DateTime? searchDateTo, 
            bool showOnlyOpenOrders = true, int page = 1, int pageSize = 10)
        {
            // Build the query for room service orders
            var query = _context.RoomServices
                .Include(r => r.Booking)
                .Include(r => r.Booking.Customer)
                .Include(r => r.Booking.Room)
                .AsQueryable();

            // Apply open orders filter if needed
            if (showOnlyOpenOrders)
            {
                query = query.Where(r => 
                    r.Status == RoomServiceStatus.Requested || 
                    r.Status == RoomServiceStatus.InProgress);
            }

            // Apply search filters
            if (!string.IsNullOrEmpty(searchRoomNumber))
            {
                query = query.Where(r => r.Booking.Room.RoomNumber.Contains(searchRoomNumber));
            }

            if (searchDateFrom.HasValue)
            {
                query = query.Where(r => r.RequestDate >= searchDateFrom.Value);
            }

            if (searchDateTo.HasValue)
            {
                query = query.Where(r => r.RequestDate <= searchDateTo.Value);
            }

            // Get total count for pagination
            var totalOrders = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            // Apply pagination and ordering
            var orders = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoomServiceOrderItem
                {
                    RoomServiceId = r.RoomServiceId,
                    BookingId = r.BookingId,
                    CustomerName = r.Booking.Customer.FirstName + " " + r.Booking.Customer.LastName,
                    RoomNumber = r.Booking.Room.RoomNumber,
                    ItemName = r.ItemName,
                    ServiceType = r.ServiceType.ToString(),
                    Status = r.Status.ToString(),
                    RequestDate = r.RequestDate,
                    DeliveryDate = r.DeliveryDate,
                    Price = r.Price,
                    SpecialInstructions = r.SpecialInstructions ?? string.Empty
                })
                .ToListAsync();

            // Create view model
            var viewModel = new RoomServiceViewModel
            {
                Orders = orders,
                ShowOnlyOpenOrders = showOnlyOpenOrders,
                SearchRoomNumber = searchRoomNumber ?? string.Empty,
                SearchDateFrom = searchDateFrom,
                SearchDateTo = searchDateTo,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalOrders = totalOrders,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: RoomService/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.RoomServices
                .Include(r => r.Booking)
                .Include(r => r.Booking.Customer)
                .Include(r => r.Booking.Room)
                .FirstOrDefaultAsync(m => m.RoomServiceId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: RoomService/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.RoomServices.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<RoomServiceStatus>(status, out var orderStatus))
            {
                order.Status = orderStatus;
                if (orderStatus == RoomServiceStatus.Delivered)
                {
                    order.DeliveryDate = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order status updated to {status} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status provided.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
