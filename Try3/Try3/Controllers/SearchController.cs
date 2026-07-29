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
            string? neighborhood,
            decimal? minPrice,
            decimal? maxPrice,

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
                    .Where(p => p.IsActive == true);

                // 2. تطبيق الفلاتر (فقط إذا كان المستخدم قد أدخل قيمة)
                if (cityId.HasValue && cityId.Value > 0)
                    query = query.Where(p => p.CityId == cityId.Value);

                if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
                    query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);


                // 3. جلب أول 10 نتائج مع الصورة الرئيسية فقط
                var results = await query
                    .OrderBy(p => p.Price)
                    .Take(10)
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
                        Neighborhood = p.Neighborhood,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,

                    })
                    .ToListAsync();

                // 4. إذا لم تكن هناك نتائج
                if (results.Count == 0)
                {
                    ViewBag.NoDataMessage = "لم نجد عقارات تطابق معايير البحث. حاول تعديل الفلاتر.";
                }

                return PartialView("_PropertyResults", results);
            }
            catch (Exception ex)
            {
                ViewBag.NoDataMessage = "حدث خطأ: " + ex.Message;
                return PartialView("_PropertyResults", new List<Property>());
            }
        }
    }
}