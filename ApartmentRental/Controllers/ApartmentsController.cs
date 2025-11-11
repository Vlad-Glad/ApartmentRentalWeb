using ApartmentRental.Data;
using ApartmentRental.Models;
using ApartmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApartmentRental.Controllers
{

    [Authorize]
    public class ApartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBlobService _blobService;

        public ApartmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBlobService blobService )
        {
            _context = context;
            _userManager = userManager;
            _blobService = blobService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? city)
        {
            var apartmentsQuery = _context.Apartments
                .Include(a => a.Lessor)
                .AsQueryable();

            var cities = await _context.Apartments
                .Where(a => a.City != null)
                .Select(a => a.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            if (!string.IsNullOrEmpty(city))
            {
                apartmentsQuery = apartmentsQuery.Where(a => a.City == city);
            }

            ViewBag.Cities = new SelectList(cities);
            ViewBag.SelectedCity = city;
            ViewBag.Cities = new SelectList(cities, city);


            var apartments = await apartmentsQuery.ToListAsync();
            return View(apartments);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apartment == null)
            {
                return NotFound();
            }

            return View(apartment);
        }

        public IActionResult Create()
        {
            return View();
        }

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
                    "Будь ласка, натисніть «Пошук адреси» і оберіть коректну адресу з підказки.");
            }


            if (!ModelState.IsValid)
            {
                return View(apartment);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            apartment.LessorId = userId;

            _context.Add(apartment);
            await _context.SaveChangesAsync();

            if (photos != null && photos.Count > 0)
            {
                foreach (var file in photos)
                {
                    if (file.Length == 0) continue;

                    using var stream = file.OpenReadStream();
                    var url = await _blobService.UploadAsync(stream, file.FileName, file.ContentType);

                    var photo = new Photo
                    {
                        ApartmentId = apartment.Id,
                        ImageUrl = url
                    };

                    _context.Photos.Add(photo);
                }
                await _context.SaveChangesAsync();
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

            if (apartment.LessorId != currentUserId)
            {
                return Forbid();
            }

            return View(apartment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Apartment apartment, List<IFormFile> photos, bool IsAddressValid)
        {
            if (id != apartment.Id)
            {
                return NotFound();
            }

            ModelState.Remove("LessorId");
            ModelState.Remove("Lessor");
            ModelState.Remove("Photos");

            if (!IsAddressValid || apartment.Latitude == null || apartment.Longitude == null)
            {
                ModelState.AddModelError("FullAddress",
                    "Будь ласка, натисніть «Пошук адреси» і оберіть коректну адресу з підказки.");
            }

            var apartmentToUpdate = await _context.Apartments.FindAsync(id);
            if (apartmentToUpdate == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (apartmentToUpdate.LessorId != currentUserId)
            {
                return Forbid();
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

                if (photos != null && photos.Count > 0)
                {

                    if (apartmentToUpdate.Photos != null && apartmentToUpdate.Photos.Any())
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

                        var photo = new Photo
                        {
                            ApartmentId = apartmentToUpdate.Id,
                            ImageUrl = url
                        };

                        _context.Photos.Add(photo);
                    }

                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApartmentExists(apartment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var apartment = await _context.Apartments.FindAsync(id);

            if (apartment == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            if (apartment.LessorId != currentUserId)
            {
                return Forbid();
            }

            return View(apartment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (apartment.LessorId != currentUserId)
            {
                return Forbid();
            }

            if (apartment.Photos != null && apartment.Photos.Any())
            {
                foreach (var photo in apartment.Photos)
                {
                    await _blobService.DeleteAsync(photo.ImageUrl);
                }
            }

            _context.Apartments.Remove(apartment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.Id == id);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Landing(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Lessor)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
            {
                return NotFound();
            }

            return View(apartment);
        }

        
        public IActionResult Map()
        {
            return View();
        }
    }
}
