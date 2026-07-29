using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers
{
    // ✅ أضف هذا السطر
    [Route("PropertiesByCity")]
    public class PropertiesByCityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertiesByCityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ أضف هذا السطر
        [Route("City/{cityName}")]
        public async Task<IActionResult> City(string cityName)
        {
            if (string.IsNullOrEmpty(cityName))
            {
                return RedirectToAction("Index", "Home");
            }

            var properties = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Images)
                .Include(p => p.Advertiser)
                .Where(p => p.City != null && p.City.NameAr == cityName && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var city = await _context.Cities.FirstOrDefaultAsync(c => c.NameAr == cityName);

            ViewBag.CityName = city?.NameAr ?? cityName;
            ViewBag.PropertyCount = properties.Count;

            return View(properties);
        }
    }
}