using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApartmentRental.Data;
using ApartmentRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ApartmentRental.Controllers
{

    [Authorize]
    public class ApartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApartmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        public async Task<IActionResult> Create(Apartment apartment, bool IsAddressValid)
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

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Apartment apartment, bool IsAddressValid)
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
            var apartment = await _context.Apartments.FindAsync(id);

            if (apartment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (apartment.LessorId != currentUserId)
            {
                return Forbid();
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
