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

        // ===== صفحة البحث =====
        [HttpGet]
        public async Task<IActionResult> Index(string city)
        {
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.NameAr).ToListAsync();
            ViewBag.PropertyTypes = await _context.PropertyTypes.OrderBy(p => p.NameAr).ToListAsync();
            ViewBag.SelectedCity = city ?? "";
            return View();
        }

        // ===== تنفيذ البحث (AJAX) =====
        [HttpGet]
        public async Task<IActionResult> Search(
            int? propertyTypeId,
            string? status,
            int? cityId,
            string? neighborhood,
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
            try
            {
                // 1. الاستعلام الأساسي (جميع العقارات النشطة)
                var query = _context.Properties
                    .Include(p => p.City)
                    .Include(p => p.PropertyType)
                    .Include(p => p.Images)
                    .Where(p => p.IsActive == true);

                // 2. تطبيق الفلاتر
                if (cityId.HasValue && cityId.Value > 0)
                    query = query.Where(p => p.CityId == cityId.Value);

                if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
                    query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(p => p.Status == status);

                if (!string.IsNullOrEmpty(neighborhood))
                    query = query.Where(p => p.Neighborhood.Contains(neighborhood));

                if (minPrice.HasValue)
                    query = query.Where(p => p.Price >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(p => p.Price <= maxPrice.Value);

                if (!string.IsNullOrEmpty(currency))
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

                // 3. جلب النتائج
                var results = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new Property
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Title = p.Title,
                        Price = p.Price,
                        PriceCurrency = p.PriceCurrency,
                        Area = p.Area,
                        Rooms = p.Rooms,
                        Bathrooms = p.Bathrooms,
                        Floor = p.Floor,
                        Status = p.Status,
                        CityId = p.CityId,
                        City = p.City != null ? new City { Id = p.City.Id, NameAr = p.City.NameAr } : null,
                        PropertyType = p.PropertyType != null ? new PropertyType { Id = p.PropertyType.Id, NameAr = p.PropertyType.NameAr } : null,
                        Neighborhood = p.Neighborhood,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        Images = p.Images != null ? p.Images.Select(i => new PropertyImage { ImageUrl = i.ImageUrl, IsMain = i.IsMain }).ToList() : new List<PropertyImage>()
                    })
                    .ToListAsync();

                return PartialView("_PropertyResults", results);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}