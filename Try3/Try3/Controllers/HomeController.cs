using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using RealEstateWebApp.Data;      // ✅ تم التعديل حسب اسم مشروعك
using RealEstateWebApp.Models;    // ✅ تم التعديل حسب اسم مشروعك

namespace RealEstateWebApp.Controllers // ✅ تم التعديل حسب اسم مشروعك
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // الصفحة الرئيسية - تعرض المدن من قاعدة البيانات
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // جلب جميع المدن من قاعدة البيانات
            var cities = await _context.Cities.ToListAsync();
            return View(cities);
        }

        // ============================================================
        // معالجة نموذج التواصل (سنفعّله لاحقاً، لكنه موجود حتى لا تظهر أخطاء 404)
        // ============================================================
        [HttpPost]
        public IActionResult Contact(string name, string email, string message)
        {
            // هنا سنخزن الرسالة في قاعدة البيانات لاحقاً
            // حالياً نعيد رسالة نجاح وهمية للاختبار
            return Json(new { success = true, message = "تم إرسال رسالتك بنجاح" });
        }
    }
}