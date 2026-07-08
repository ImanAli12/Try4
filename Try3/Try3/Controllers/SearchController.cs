using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateWebApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== عرض صفحة البحث =====
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.PropertyTypes = await _context.PropertyTypes.OrderBy(p => p.NameAr).ToListAsync();
            return View();
        }

        // ===== تنفيذ البحث (AJAX) =====
        [HttpGet]
        public async Task<IActionResult> Search(
            int? propertyTypeId,
            string? status,
            int? cityId,
            string? neighborhood,  // ✅ تغيير من int? إلى string?
            decimal? minPrice,
            decimal? maxPrice,
            string? currency,
            decimal? minArea,
            decimal? maxArea,
            byte? rooms,
            byte? bathrooms,
            short? floor,
            short? totalFloors)
        {
            var query = _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Images)
                .Where(p => p.IsActive == true && p.Status == "للبيع");

            // تطبيق الفلاتر
            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
                query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);

            if (cityId.HasValue && cityId.Value > 0)
                query = query.Where(p => p.CityId == cityId.Value);

            // ✅ البحث عن الحي كنص
            if (!string.IsNullOrWhiteSpace(neighborhood))
                query = query.Where(p => p.Neighborhood != null && p.Neighborhood.Contains(neighborhood));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(currency))
                query = query.Where(p => p.PriceCurrency == currency);

            if (minArea.HasValue)
                query = query.Where(p => p.Area >= minArea.Value);

            if (maxArea.HasValue)
                query = query.Where(p => p.Area <= maxArea.Value);

            if (rooms.HasValue && rooms.Value > 0)
                query = query.Where(p => p.Rooms == rooms.Value);

            if (bathrooms.HasValue && bathrooms.Value > 0)
                query = query.Where(p => p.Bathrooms == bathrooms.Value);

            if (floor.HasValue && floor.Value >= 0)
                query = query.Where(p => p.Floor == floor.Value);

            // ترتيب حسب الأحدث
            var results = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return PartialView("_PropertyResults", results);
        }
    }
}