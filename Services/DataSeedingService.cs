using ContosoHotels.Data;
using ContosoHotels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContosoHotels.Services
{
    public class DataSeedingService
    {
        private readonly ContosoHotelsContext _context;
        private readonly Random _random = new Random(42); // Fixed seed for consistent data

        public DataSeedingService(ContosoHotelsContext context)
        {
            _context = context;
        }

        public async Task SeedDataAsync()
        {
            await _context.Database.MigrateAsync();

            if (!_context.Customers.Any())
            {
                await SeedCustomersAsync();
            }

            if (!_context.Rooms.Any())
            {
                await SeedRoomsAsync();
            }

            if (!_context.Bookings.Any())
            {
                await SeedBookingsAsync();
            }

            if (!_context.RoomServices.Any())
            {
                await SeedRoomServicesAsync();
            }

            if (!_context.HousekeepingRequests.Any())
            {
                await SeedHousekeepingRequestsAsync();
            }
        }

        private async Task SeedCustomersAsync()
        {
            var customers = new List<Customer>();
            var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Jessica", "William", "Ashley", "James", "Amanda", "Christopher", "Melissa", "Daniel", "Deborah", "Matthew", "Dorothy", "Anthony", "Lisa" };
            var lastNames = new[] { "Smith", "Johnson", "Brown", "Taylor", "Anderson", "Wilson", "Moore", "Jackson", "Martin", "Lee", "Thompson", "White", "Lopez", "Clark", "Rodriguez", "Lewis", "Walker", "Hall", "Allen", "Young" };
            var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
            var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "CA" };

            for (int i = 0; i < 200; i++)
            {
                var firstName = firstNames[_random.Next(firstNames.Length)];
                var lastName = lastNames[_random.Next(lastNames.Length)];
                var cityIndex = _random.Next(cities.Length);

                customers.Add(new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@email.com",
                    PhoneNumber = $"+1-{_random.Next(100, 999)}-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}",
                    Address = $"{_random.Next(1, 9999)} {lastNames[_random.Next(lastNames.Length)]} St",
                    City = cities[cityIndex],
                    State = states[cityIndex],
                    ZipCode = _random.Next(10000, 99999).ToString(),
                    Country = "USA",
                    CreatedDate = DateTime.UtcNow.AddDays(-_random.Next(1, 1825)) // Random date within last 5 years
                });
            }

            _context.Customers.AddRange(customers);
            await _context.SaveChangesAsync();
        }        private async Task SeedRoomsAsync()
        {
            var rooms = new List<Room>();
            var roomTypes = new[] { "Standard", "Deluxe", "Suite", "Penthouse", "Family", "Business" };
            var descriptions = new Dictionary<string, string>
            {
                { "Standard", "Comfortable room with all basic amenities for a pleasant stay." },
                { "Deluxe", "Spacious room with premium amenities and modern furnishings." },
                { "Suite", "Luxurious suite with separate living area and enhanced services." },
                { "Penthouse", "Ultimate luxury with panoramic views and exclusive amenities." },
                { "Family", "Perfect for families with extra space and child-friendly features." },
                { "Business", "Designed for business travelers with work area and high-speed internet." }
            };

            var cities = new[] { 
                "New York", "Miami", "Los Angeles", "San Francisco", "Chicago", 
                "Boston", "Seattle", "Denver", "Austin", "San Diego"
            };

            var basePrices = new Dictionary<string, decimal>
            {
                { "Standard", 99m },
                { "Deluxe", 149m },
                { "Suite", 249m },
                { "Penthouse", 499m },
                { "Family", 179m },
                { "Business", 189m }
            };

            for (int floor = 1; floor <= 10; floor++)
            {                for (int roomNum = 1; roomNum <= 20; roomNum++)
                {                    
                    var roomNumber = $"{floor:D2}{roomNum:D2}";
                    var roomType = roomTypes[_random.Next(roomTypes.Length)];
                    var city = cities[_random.Next(cities.Length)];
                    var hasOceanView = floor >= 5 && _random.Next(1, 4) == 1; // Higher floors more likely to have ocean view
                    var priceMultiplier = hasOceanView ? 1.3m : 1.0m;
                    var hasMinibar = roomType != "Standard";
                    var hasBalcony = floor >= 3 && _random.Next(1, 3) == 1;

                    // Generate amenities string based on room features
                    var amenities = new List<string> { "Free WiFi", "Air Conditioning", "Flat-screen TV" };
                    if (hasMinibar) amenities.Add("Minibar");
                    if (hasBalcony) amenities.Add("Private Balcony");
                    if (hasOceanView) amenities.Add("Ocean View");
                    if (roomType == "Suite" || roomType == "Penthouse") amenities.Add("Separate Living Area");
                    if (roomType == "Business") amenities.Add("Work Desk");
                    if (roomType == "Family") amenities.Add("Extra Beds");

                    rooms.Add(new Room
                    {
                        RoomNumber = roomNumber,
                        RoomType = roomType,
                        City = city,
                        Description = descriptions[roomType],
                        PricePerNight = basePrices[roomType] * priceMultiplier,
                        MaxOccupancy = roomType == "Suite" || roomType == "Penthouse" ? 4 : roomType == "Family" ? 6 : 2,
                        HasWifi = true,
                        HasAirConditioning = true,
                        HasTv = true,
                        HasMinibar = hasMinibar,
                        HasBalcony = hasBalcony,
                        HasOceanView = hasOceanView,
                        Amenities = string.Join(", ", amenities),
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow.AddDays(-_random.Next(1, 365))
                    });
                }
            }

            _context.Rooms.AddRange(rooms);
            await _context.SaveChangesAsync();
        }

        private async Task SeedBookingsAsync()
        {
            var customers = await _context.Customers.ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();
            var bookings = new List<Booking>();
            var bookingStatuses = Enum.GetValues(typeof(BookingStatus)).Cast<BookingStatus>().ToArray();

            // Generate bookings for the past 5 years
            var startDate = DateTime.UtcNow.AddYears(-5);
            var endDate = DateTime.UtcNow.AddMonths(6); // Include future bookings

            for (int i = 0; i < 1000; i++)
            {
                var customer = customers[_random.Next(customers.Count)];
                var room = rooms[_random.Next(rooms.Count)];
                
                var checkInDate = startDate.AddDays(_random.Next(0, (int)(endDate - startDate).TotalDays));
                var stayDuration = _random.Next(1, 8); // 1-7 nights
                var checkOutDate = checkInDate.AddDays(stayDuration);
                
                var status = checkOutDate < DateTime.UtcNow ? 
                    (_random.Next(1, 11) == 1 ? BookingStatus.Cancelled : BookingStatus.CheckedOut) :
                    checkInDate <= DateTime.UtcNow ? BookingStatus.CheckedIn :
                    bookingStatuses[_random.Next(0, 3)]; // Pending, Confirmed, or Cancelled for future

                var totalAmount = room.PricePerNight * stayDuration;                bookings.Add(new Booking
                {
                    CustomerId = customer.CustomerId,
                    RoomId = room.RoomId,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    NumberOfGuests = _random.Next(1, room.MaxOccupancy + 1),
                    TotalAmount = totalAmount,
                    Status = status,
                    SpecialRequests = _random.Next(1, 5) == 1 ? GetRandomSpecialRequest() : null,
                    BookingDate = checkInDate.AddDays(-_random.Next(1, 30)), // Booked 1-30 days before check-in
                    CancellationDate = status == BookingStatus.Cancelled ? (DateTime?)checkInDate.AddDays(-_random.Next(1, 10)) : null,
                    CancellationReason = status == BookingStatus.Cancelled ? GetRandomCancellationReason() : null
                });
            }

            _context.Bookings.AddRange(bookings);
            await _context.SaveChangesAsync();
        }

        private string GetRandomSpecialRequest()
        {
            var requests = new[]
            {
                "Late check-in after 10 PM",
                "Non-smoking room preferred",
                "High floor room requested",
                "Extra towels needed",
                "Room close to elevator",
                "Quiet room away from elevator",
                "Twin beds instead of double",
                "Extra pillows requested",
                "Airport shuttle service needed",
                "Early check-in if possible"
            };

            return requests[_random.Next(requests.Length)];
        }

        private string GetRandomCancellationReason()
        {
            var reasons = new[]
            {
                "Change of travel plans",
                "Flight cancelled",
                "Medical emergency",
                "Work commitment",
                "Found better accommodation",
                "Personal reasons",
                "Weather conditions",
                "Family emergency"
            };

            return reasons[_random.Next(reasons.Length)];
        }

        private async Task SeedRoomServicesAsync()
        {
            var bookings = await _context.Bookings
                .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut)
                .ToListAsync();
            
            var roomServices = new List<RoomService>();
            var serviceTypes = Enum.GetValues(typeof(RoomServiceType)).Cast<RoomServiceType>().ToArray();
            var serviceStatuses = Enum.GetValues(typeof(RoomServiceStatus)).Cast<RoomServiceStatus>().ToArray();

            // Food menu items
            var foodItems = new Dictionary<string, decimal>
            {
                { "Club Sandwich", 18.95m },
                { "Caesar Salad", 15.50m },
                { "Cheeseburger & Fries", 22.75m },
                { "Pasta Carbonara", 19.95m },
                { "Margherita Pizza", 20.50m },
                { "Grilled Salmon", 29.95m },
                { "Steak & Vegetables", 34.95m },
                { "Chicken Quesadilla", 17.95m }
            };

            // Beverage menu items
            var beverageItems = new Dictionary<string, decimal>
            {
                { "Bottled Water", 4.50m },
                { "Soda", 5.25m },
                { "Coffee", 6.50m },
                { "Tea", 5.95m },
                { "Fresh Juice", 7.95m },
                { "Smoothie", 9.50m },
                { "Glass of Wine", 12.95m },
                { "Beer", 8.95m },
                { "Cocktail", 14.95m }
            };

            // Amenity items
            var amenityItems = new Dictionary<string, decimal>
            {
                { "Extra Toiletries", 0m },
                { "Dental Kit", 3.95m },
                { "Shaving Kit", 4.95m },
                { "Sewing Kit", 2.95m },
                { "Phone Charger", 15.95m },
                { "Extra Blanket", 0m },
                { "Extra Pillow", 0m },
                { "Bathrobe", 0m }
            };

            // Add room service for 30% of eligible bookings
            foreach (var booking in bookings)
            {
                // Only create room service for 30% of bookings
                if (_random.Next(1, 101) > 30)
                    continue;

                // Generate 1-3 room service orders per booking
                int ordersCount = _random.Next(1, 4);
                
                for (int i = 0; i < ordersCount; i++)
                {
                    // Determine service type
                    var serviceType = serviceTypes[_random.Next(serviceTypes.Length)];
                    
                    string itemName;
                    decimal price;
                    string description = null;

                    // Set item and price based on service type
                    switch (serviceType)
                    {
                        case RoomServiceType.Food:
                            var foodItem = foodItems.ElementAt(_random.Next(foodItems.Count));
                            itemName = foodItem.Key;
                            price = foodItem.Value;
                            break;
                        case RoomServiceType.Beverage:
                            var bevItem = beverageItems.ElementAt(_random.Next(beverageItems.Count));
                            itemName = bevItem.Key;
                            price = bevItem.Value;
                            break;
                        case RoomServiceType.Amenity:
                            var amenItem = amenityItems.ElementAt(_random.Next(amenityItems.Count));
                            itemName = amenItem.Key;
                            price = amenItem.Value;
                            break;
                        default: // Other
                            itemName = "Special Request";
                            price = _random.Next(10, 50);
                            description = "Custom room service request";
                            break;
                    }                    // Determine dates
                    var orderDate = booking.CheckInDate.AddHours(_random.Next(1, (int)(booking.CheckOutDate - booking.CheckInDate).TotalHours));
                    DateTime? deliveryDate = null;
                    if (booking.Status == BookingStatus.CheckedOut)
                    {
                        deliveryDate = orderDate.AddMinutes(_random.Next(15, 60));
                    }
                    
                    // Determine status
                    var status = booking.Status == BookingStatus.CheckedOut ? 
                        RoomServiceStatus.Delivered : 
                        serviceStatuses[_random.Next(serviceStatuses.Length)];

                    if (status == RoomServiceStatus.Delivered && !deliveryDate.HasValue)
                    {
                        deliveryDate = orderDate.AddMinutes(_random.Next(15, 60));
                    }

                    // Add special instructions for ~20% of orders
                    string specialInstructions = null;
                    if (_random.Next(1, 101) <= 20)
                    {
                        var instructions = new[]
                        {
                            "Please deliver after 8 PM",
                            "Extra ice in drinks",
                            "No onions in the food",
                            "Allergic to nuts",
                            "Gluten-free option if available",
                            "Extra sauce on the side",
                            "Well done steak",
                            "Leave outside the door"
                        };
                        specialInstructions = instructions[_random.Next(instructions.Length)];
                    }

                    roomServices.Add(new RoomService
                    {
                        BookingId = booking.BookingId,
                        ItemName = itemName,
                        ItemDescription = description,
                        ServiceType = serviceType,
                        Status = status,
                        RequestDate = orderDate,
                        DeliveryDate = deliveryDate,
                        Price = price,
                        SpecialInstructions = specialInstructions
                    });
                }
            }

            _context.RoomServices.AddRange(roomServices);
            await _context.SaveChangesAsync();
        }

        private async Task SeedHousekeepingRequestsAsync()
        {
            var bookings = await _context.Bookings
                .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.CheckedOut)
                .ToListAsync();
            
            var requests = new List<HousekeepingRequest>();
            var requestTypes = Enum.GetValues(typeof(HousekeepingRequestType)).Cast<HousekeepingRequestType>().ToArray();
            var requestStatuses = Enum.GetValues(typeof(HousekeepingRequestStatus)).Cast<HousekeepingRequestStatus>().ToArray();

            // Add housekeeping requests for 20% of eligible bookings
            foreach (var booking in bookings)
            {
                // Only create housekeeping requests for 20% of bookings
                if (_random.Next(1, 101) > 20)
                    continue;

                // Generate 1-2 housekeeping requests per booking
                int requestsCount = _random.Next(1, 3);
                
                for (int i = 0; i < requestsCount; i++)
                {
                    // Determine request type (weighted to have more towel requests)
                    var requestType = _random.Next(1, 11) <= 6 ? 
                        HousekeepingRequestType.Towels : 
                        requestTypes[_random.Next(requestTypes.Length)];                    // Determine dates
                    var requestDate = booking.CheckInDate.AddHours(_random.Next(1, (int)(booking.CheckOutDate - booking.CheckInDate).TotalHours));
                    DateTime? completionDate = null;
                    if (booking.Status == BookingStatus.CheckedOut)
                    {
                        completionDate = requestDate.AddMinutes(_random.Next(15, 60));
                    }
                    
                    // Determine status
                    var status = booking.Status == BookingStatus.CheckedOut ? 
                        HousekeepingRequestStatus.Completed : 
                        requestStatuses[_random.Next(requestStatuses.Length)];

                    if (status == HousekeepingRequestStatus.Completed && !completionDate.HasValue)
                    {
                        completionDate = requestDate.AddMinutes(_random.Next(15, 60));
                    }

                    // Add notes for ~30% of requests
                    string notes = null;
                    if (_random.Next(1, 101) <= 30)
                    {
                        var noteOptions = new[]
                        {
                            "Please deliver to room as soon as possible",
                            "Extra towels needed for children",
                            "Need fresh bed sheets",
                            "Prefer quick service before 2PM",
                            "Please knock before entering",
                            "Do not disturb until called",
                            "Only hand towels needed"
                        };
                        notes = noteOptions[_random.Next(noteOptions.Length)];
                    }

                    requests.Add(new HousekeepingRequest
                    {
                        BookingId = booking.BookingId,
                        RequestType = requestType,
                        Status = status,
                        RequestDate = requestDate,
                        CompletionDate = completionDate,
                        Notes = notes
                    });
                }
            }

            _context.HousekeepingRequests.AddRange(requests);
            await _context.SaveChangesAsync();
        }
    }
}
