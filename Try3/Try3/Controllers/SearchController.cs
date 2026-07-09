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
        // GET: Properties/Details/{code}
        [HttpGet]
        public async Task<IActionResult> Details(string code)
        {
            if (string.IsNullOrEmpty(code))
                return NotFound();

            var property = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (property == null)
                return NotFound();

            // ✅ هذا هو الاستعلام الذي كان يعمل عندما اشتغلت الخوارزمية
            var similarCodes = await _context.SimilarProperties
                .Where(sp => sp.PropertyCode == code)
                .OrderBy(sp => sp.RankOrder)
                .Select(sp => sp.SimilarPropertyCode)
                .Take(10)
                .ToListAsync();

            var similarProperties = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Images)
                .Where(p => similarCodes.Contains(p.Code))
                .ToListAsync();

            var orderedSimilar = similarProperties
                .Select(p => new { Property = p, Index = similarCodes.IndexOf(p.Code) })
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Index)
                .Select(x => x.Property)
                .ToList();

            ViewBag.SimilarProperties = orderedSimilar;
            return View(property);
        }
    }
}