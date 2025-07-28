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
    public class BookingsController : Controller
    {
        private readonly ContosoHotelsContext _context;

        public BookingsController(ContosoHotelsContext context)
        {
            _context = context;
        }

        // GET: Bookings/Create?roomId=1&checkIn=2023-01-01&checkOut=2023-01-03&guests=2
        public async Task<IActionResult> Create(int roomId, DateTime checkIn, DateTime checkOut, int guests = 2)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return NotFound();
            }

            var viewModel = new BookingViewModel
            {
                RoomId = roomId,
                Room = room,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = guests
            };

            return View(viewModel);
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Room = await _context.Rooms.FindAsync(model.RoomId);
                return View(model);
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
                model.Room = await _context.Rooms.FindAsync(model.RoomId);
                return View(model);
            }

            // Check if room is still available
            var isRoomAvailable = await IsRoomAvailableAsync(model.RoomId, model.CheckInDate, model.CheckOutDate);
            if (!isRoomAvailable)
            {
                ModelState.AddModelError("", "Sorry, this room is no longer available for the selected dates.");
                model.Room = await _context.Rooms.FindAsync(model.RoomId);
                return View(model);
            }

            // Find or create customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Create booking
            var room = await _context.Rooms.FindAsync(model.RoomId);
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                RoomId = model.RoomId,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                NumberOfGuests = model.NumberOfGuests,
                TotalAmount = room.PricePerNight * (decimal)(model.CheckOutDate - model.CheckInDate).TotalDays,
                Status = BookingStatus.Confirmed,
                SpecialRequests = model.SpecialRequests,
                BookingDate = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = booking.BookingId });
        }

        // GET: Bookings/Confirmation/5
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

        // GET: Bookings/Search
        public IActionResult Search()
        {
            return View();
        }

        // POST: Bookings/Search
        [HttpPost]
        public async Task<IActionResult> Search(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter an email address.");
                return View();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => b.Customer.Email == email)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View("SearchResults", bookings);
        }

        // GET: Bookings/MyBookings/{email}
        public async Task<IActionResult> MyBookings(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Search");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => b.Customer.Email == email)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            if (!bookings.Any())
            {
                TempData["ErrorMessage"] = "No bookings found for this email address.";
                return RedirectToAction("Search");
            }

            ViewBag.CustomerEmail = email;
            return View(bookings);
        }        // GET: Bookings/Manage/{id}
        public async Task<IActionResult> Manage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Get room service orders for this booking
            var roomServices = await _context.RoomServices
                .Where(rs => rs.BookingId == id)
                .OrderByDescending(rs => rs.RequestDate)
                .ToListAsync();

            // Get housekeeping requests for this booking
            var housekeepingRequests = await _context.HousekeepingRequests
                .Where(hr => hr.BookingId == id)
                .OrderByDescending(hr => hr.RequestDate)
                .ToListAsync();

            ViewData["RoomServices"] = roomServices;
            ViewData["HousekeepingRequests"] = housekeepingRequests;

            return View(booking);
        }

        // POST: Bookings/Cancel/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancellationReason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if cancellation is allowed (e.g., not already cancelled, not checked in)
            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "This booking is already cancelled.";
                return RedirectToAction("Manage", new { id = booking.BookingId });
            }

            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
            {
                TempData["ErrorMessage"] = "Cannot cancel a booking that has already been checked in.";
                return RedirectToAction("Manage", new { id = booking.BookingId });
            }

            // Calculate cancellation fee if within 24 hours
            var hoursUntilCheckIn = (booking.CheckInDate - DateTime.UtcNow).TotalHours;
            if (hoursUntilCheckIn < 24 && hoursUntilCheckIn > 0)
            {
                TempData["WarningMessage"] = "Cancellation within 24 hours may incur charges. Please contact customer service for details.";
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationDate = DateTime.UtcNow;
            booking.CancellationReason = cancellationReason ?? "Cancelled by guest";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking #{booking.BookingId} has been successfully cancelled.";
            return RedirectToAction("MyBookings", new { email = booking.Customer.Email });
        }

        // GET: Bookings/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Check if editing is allowed
            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Cannot edit a cancelled booking.";
                return RedirectToAction("Manage", new { id = booking.BookingId });
            }

            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
            {
                TempData["ErrorMessage"] = "Cannot edit a booking that has already been checked in.";
                return RedirectToAction("Manage", new { id = booking.BookingId });
            }

            // Create edit view model
            var editModel = new BookingEditViewModel
            {
                BookingId = booking.BookingId,
                CurrentCheckInDate = booking.CheckInDate,
                CurrentCheckOutDate = booking.CheckOutDate,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                SpecialRequests = booking.SpecialRequests,
                Room = booking.Room,
                Customer = booking.Customer,
                CurrentTotalAmount = booking.TotalAmount
            };

            return View(editModel);
        }

        // POST: Bookings/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookingEditViewModel model)
        {
            if (id != model.BookingId)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
                model.Room = booking.Room;
                model.Customer = booking.Customer;
                return View(model);
            }

            // Check if room is available for new dates (excluding current booking)
            var isRoomAvailable = await IsRoomAvailableForEditAsync(booking.RoomId, model.CheckInDate, model.CheckOutDate, booking.BookingId);
            if (!isRoomAvailable)
            {
                ModelState.AddModelError("", "Sorry, this room is not available for the selected dates.");
                model.Room = booking.Room;
                model.Customer = booking.Customer;
                return View(model);
            }

            // Update booking
            booking.CheckInDate = model.CheckInDate;
            booking.CheckOutDate = model.CheckOutDate;
            booking.NumberOfGuests = model.NumberOfGuests;
            booking.SpecialRequests = model.SpecialRequests;
            booking.TotalAmount = booking.Room.PricePerNight * (decimal)(model.CheckOutDate - model.CheckInDate).TotalDays;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your booking has been successfully updated.";
            return RedirectToAction("Manage", new { id = booking.BookingId });
        }

        // POST: Bookings/OrderRoomService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderRoomService(int bookingId, string itemName, RoomServiceType serviceType, decimal price, string specialInstructions)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if booking is active
            if (booking.Status != BookingStatus.CheckedIn && booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "Room service can only be ordered for active bookings.";
                return RedirectToAction("Manage", new { id = bookingId });
            }

            var roomService = new RoomService
            {
                BookingId = bookingId,
                ItemName = itemName,
                ServiceType = serviceType,
                Price = price,
                Status = RoomServiceStatus.Requested,
                RequestDate = DateTime.UtcNow,
                SpecialInstructions = specialInstructions
            };

            _context.RoomServices.Add(roomService);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Room service order has been placed successfully.";
            return RedirectToAction("Manage", new { id = bookingId });
        }

        // POST: Bookings/RequestHousekeeping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestHousekeeping(int bookingId, HousekeepingRequestType requestType, string notes)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound();
            }

            // Check if booking is active
            if (booking.Status != BookingStatus.CheckedIn && booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "Housekeeping can only be requested for active bookings.";
                return RedirectToAction("Manage", new { id = bookingId });
            }

            var housekeepingRequest = new HousekeepingRequest
            {
                BookingId = bookingId,
                RequestType = requestType,
                Status = HousekeepingRequestStatus.Requested,
                RequestDate = DateTime.UtcNow,
                Notes = notes
            };

            _context.HousekeepingRequests.Add(housekeepingRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Housekeeping request has been submitted successfully.";
            return RedirectToAction("Manage", new { id = bookingId });
        }

        private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                           b.Status != BookingStatus.Cancelled &&
                           ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                            (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                            (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
                .AnyAsync();

            return !conflictingBookings;
        }

        private async Task<bool> IsRoomAvailableForEditAsync(int roomId, DateTime checkIn, DateTime checkOut, int excludeBookingId)
        {
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                           b.BookingId != excludeBookingId &&
                           b.Status != BookingStatus.Cancelled &&
                           ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                            (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                            (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
                .AnyAsync();

            return !conflictingBookings;
        }
    }
}
