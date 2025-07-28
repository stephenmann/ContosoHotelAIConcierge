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
    public class RoomsController : Controller
    {
        private readonly ContosoHotelsContext _context;

        public RoomsController(ContosoHotelsContext context)
        {
            _context = context;
        }        // GET: Rooms
        public IActionResult Index()
        {
            var searchViewModel = new RoomSearchViewModel();
            return View(searchViewModel);
        }

        // POST: Rooms/Search
        [HttpPost]
        public async Task<IActionResult> Search(RoomSearchViewModel model, int page = 1)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
                return View("Index", model);
            }

            var availableRooms = await GetAvailableRoomsAsync(model);            // Apply filters
            if (!string.IsNullOrEmpty(model.City))
            {
                availableRooms = availableRooms.Where(r => r.City.ToLower() == model.City.ToLower());
            }
              if (!string.IsNullOrEmpty(model.RoomType))
            {
                availableRooms = availableRooms.Where(r => r.RoomType.ToLower() == model.RoomType.ToLower());
            }

            if (model.MaxPrice.HasValue)
            {
                availableRooms = availableRooms.Where(r => r.PricePerNight <= model.MaxPrice.Value);
            }

            if (model.OceanView)
            {
                availableRooms = availableRooms.Where(r => r.HasOceanView);
            }

            if (model.Balcony)
            {
                availableRooms = availableRooms.Where(r => r.HasBalcony);
            }

            if (model.Minibar)
            {
                availableRooms = availableRooms.Where(r => r.HasMinibar);
            }

            availableRooms = availableRooms.Where(r => r.MaxOccupancy >= model.NumberOfGuests);

            var totalResults = availableRooms.Count();
            var pageSize = 10;
            var pagedRooms = availableRooms
                .OrderBy(r => r.PricePerNight)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var resultsViewModel = new SearchResultsViewModel
            {
                SearchCriteria = model,
                AvailableRooms = pagedRooms,
                TotalResults = totalResults,
                PageNumber = page,
                PageSize = pageSize
            };

            return View("SearchResults", resultsViewModel);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        private async Task<IQueryable<Room>> GetAvailableRoomsAsync(RoomSearchViewModel searchModel)
        {
            // Get rooms that are not booked during the requested period
            var bookedRoomIds = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled &&
                           ((b.CheckInDate <= searchModel.CheckInDate && b.CheckOutDate > searchModel.CheckInDate) ||
                            (b.CheckInDate < searchModel.CheckOutDate && b.CheckOutDate >= searchModel.CheckOutDate) ||
                            (b.CheckInDate >= searchModel.CheckInDate && b.CheckOutDate <= searchModel.CheckOutDate)))
                .Select(b => b.RoomId)
                .ToListAsync();

            return _context.Rooms
                .Where(r => r.IsActive && !bookedRoomIds.Contains(r.RoomId));
        }
    }
}
