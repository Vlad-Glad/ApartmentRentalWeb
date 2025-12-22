using ApartmentRental.Data;
using ApartmentRental.Models;
using ApartmentRental.Search;
using ApartmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ApartmentRental.Hubs;

namespace ApartmentRental.Controllers
{
    [Authorize]
    public class ApartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBlobService _blobService;
        private readonly IApartmentSearchService _search;
        private readonly ILogger<ApartmentsController> _logger;
        private readonly IHubContext<ApartmentsHub> _hub;

        public ApartmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBlobService blobService,
            IApartmentSearchService search,
             IHubContext<ApartmentsHub> hub,
            ILogger<ApartmentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _blobService = blobService;
            _search = search;
            _hub = hub;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? city)
        {
            var apartmentsQuery = _context.Apartments
                .Include(a => a.Lessor)
                .AsQueryable();

            var cities = await _context.Apartments
                .Where(a => a.City != null)
                .Select(a => a.City!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            if (!string.IsNullOrEmpty(city))
            {
                apartmentsQuery = apartmentsQuery.Where(a => a.City == city);
            }

            ViewBag.SelectedCity = city;
            ViewBag.Cities = new SelectList(cities, city);

            var apartments = await apartmentsQuery.ToListAsync();
            return View(apartments);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (apartment == null) return NotFound();

            return View(apartment);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Apartment apartment, List<IFormFile> photos, bool IsAddressValid)
        {
            ModelState.Remove("LessorId");
            ModelState.Remove("Lessor");
            ModelState.Remove("Photos");

            if (!IsAddressValid || apartment.Latitude == null || apartment.Longitude == null)
            {
                ModelState.AddModelError("FullAddress",
                    "Please click “Find address” and select a valid address from the suggestions.");
            }

            if (apartment.Price <= 0)
            {
                ModelState.AddModelError(nameof(Apartment.Price), "Price must be greater than zero.");
            }

            if (!ModelState.IsValid)
                return View(apartment);

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var normalizedAddress = apartment.FullAddress.Trim();

            var duplicateExists = await _context.Apartments.AnyAsync(a =>
                a.LessorId == userId &&
                a.FullAddress == normalizedAddress);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(Apartment.FullAddress),
                    "An apartment with the same address already exists.");
                return View(apartment);
            }

            apartment.LessorId = userId;
            apartment.FullAddress = normalizedAddress;

            _context.Add(apartment);
            await _context.SaveChangesAsync();

            // Upload photos (optional)
            if (photos is { Count: > 0 })
            {
                foreach (var file in photos)
                {
                    if (file.Length == 0) continue;

                    using var stream = file.OpenReadStream();
                    var url = await _blobService.UploadAsync(stream, file.FileName, file.ContentType);

                    _context.Photos.Add(new Photo
                    {
                        ApartmentId = apartment.Id,
                        ImageUrl = url
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Azure Search indexing
            try
            {
                await IndexApartmentAsync(apartment, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Search indexing failed for apartment {ApartmentId}", apartment.Id);
            }

            // SignalR event
            try
            {
                await _hub.Clients.All.SendAsync("apartmentChanged", new
                {
                    action = "created",
                    apartmentId = apartment.Id,
                    city = apartment.City
                }, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR notify failed for apartment {ApartmentId}", apartment.Id);
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var apartment = await _context.Apartments
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (apartment.LessorId != currentUserId) return Forbid();

            return View(apartment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Apartment apartment, List<IFormFile> photos, bool IsAddressValid)
        {
            if (id != apartment.Id) return NotFound();

            ModelState.Remove("LessorId");
            ModelState.Remove("Lessor");
            ModelState.Remove("Photos");

            if (!IsAddressValid || apartment.Latitude == null || apartment.Longitude == null)
            {
                ModelState.AddModelError("FullAddress",
                    "Please click “Find address” and select a valid address from the suggestions.");
            }

            var apartmentToUpdate = await _context.Apartments
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartmentToUpdate == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (apartmentToUpdate.LessorId != currentUserId) return Forbid();

            var normalizedAddress = apartment.FullAddress.Trim();

            var duplicateExists = await _context.Apartments.AnyAsync(a =>
                a.Id != apartment.Id &&
                a.LessorId == currentUserId &&
                a.FullAddress == normalizedAddress);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(Apartment.FullAddress),
                    "An apartment with the same address already exists.");
                return View(apartment);
            }

            apartment.FullAddress = normalizedAddress;

            if (apartment.Price <= 0)
            {
                ModelState.AddModelError(nameof(Apartment.Price), "Price must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                return View(apartment);
            }

            try
            {
                apartmentToUpdate.Title = apartment.Title;
                apartmentToUpdate.Description = apartment.Description;
                apartmentToUpdate.Price = apartment.Price;

                apartmentToUpdate.City = apartment.City;
                apartmentToUpdate.FullAddress = apartment.FullAddress;
                apartmentToUpdate.Latitude = apartment.Latitude;
                apartmentToUpdate.Longitude = apartment.Longitude;

                if (photos is { Count: > 0 })
                {
                    if (apartmentToUpdate.Photos is { Count: > 0 })
                    {
                        foreach (var oldPhoto in apartmentToUpdate.Photos)
                        {
                            await _blobService.DeleteAsync(oldPhoto.ImageUrl);
                        }
                        _context.Photos.RemoveRange(apartmentToUpdate.Photos);
                    }

                    foreach (var file in photos)
                    {
                        if (file.Length == 0) continue;

                        using var stream = file.OpenReadStream();
                        var url = await _blobService.UploadAsync(stream, file.FileName, file.ContentType);

                        _context.Photos.Add(new Photo
                        {
                            ApartmentId = apartmentToUpdate.Id,
                            ImageUrl = url
                        });
                    }
                }

                await _context.SaveChangesAsync();

                try
                {
                    await IndexApartmentAsync(apartmentToUpdate, HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Azure Search indexing failed for apartment {ApartmentId}", apartmentToUpdate.Id);
                }

                // SignalR event
                try
                {
                    await _hub.Clients.All.SendAsync("apartmentChanged", new
                    {
                        action = "updated",
                        apartmentId = apartmentToUpdate.Id,
                        city = apartmentToUpdate.City
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SignalR notify failed for apartment {ApartmentId}", apartment.Id);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApartmentExists(apartment.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .FirstOrDefaultAsync(a => a.Id == id.Value);

            if (apartment == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (apartment.LessorId != currentUserId) return Forbid();

            return View(apartment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null) return RedirectToAction(nameof(Index));

            var currentUserId = _userManager.GetUserId(User);
            if (apartment.LessorId != currentUserId) return Forbid();

            if (apartment.Photos is { Count: > 0 })
            {
                foreach (var photo in apartment.Photos)
                {
                    await _blobService.DeleteAsync(photo.ImageUrl);
                }
            }

            _context.Apartments.Remove(apartment);
            await _context.SaveChangesAsync();

            try
            {
                await _search.DeleteAsync($"apt-{id}", HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Search delete failed for apartment {ApartmentId}", id);
            }

            // SignalR event
            try
            {
                await _hub.Clients.All.SendAsync("apartmentChanged", new
                {
                    action = "deleted",
                    apartmentId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR notify failed for apartment {ApartmentId}", apartment.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ApartmentExists(int id) => _context.Apartments.Any(e => e.Id == id);

        [AllowAnonymous]
        public async Task<IActionResult> Landing(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null) return NotFound();
            return View(apartment);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Search(string q, CancellationToken ct)
        {
            ViewBag.Query = q;

            if (string.IsNullOrWhiteSpace(q))
                return View(new List<Apartment>());

            var hits = await _search.SearchAsync(q, ct);
            var ids = hits.Select(x => x.ApartmentId).Distinct().ToList();

            if (ids.Count == 0)
                return View(new List<Apartment>());

            var apartments = await _context.Apartments
                .Include(a => a.Lessor)
                .Include(a => a.Photos)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(ct);

            var order = ids.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
            apartments = apartments.OrderBy(a => order[a.Id]).ToList();

            return View(apartments);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reindex(CancellationToken ct)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();

            var apartments = await _context.Apartments.ToListAsync(ct);

            foreach (var a in apartments)
            {
                try
                {
                    await IndexApartmentAsync(a, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Azure Search indexing failed during reindex for apartment {ApartmentId}", a.Id);
                }
            }

            return Ok(new { message = "Reindex done", count = apartments.Count });
        }

        public IActionResult Map() => View();

        private async Task IndexApartmentAsync(Apartment apartment, CancellationToken ct = default)
        {
            var lessor = await _userManager.FindByIdAsync(apartment.LessorId);
            var lessorEmail = lessor?.Email ?? "";

            var doc = new ApartmentSearchDocument
            {
                Id = $"apt-{apartment.Id}",
                ApartmentId = apartment.Id,
                Title = apartment.Title ?? "",
                LessorEmail = lessorEmail
            };

            await _search.IndexAsync(doc, ct);
        }
    }
}
