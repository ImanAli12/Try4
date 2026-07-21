using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers
{
    [Authorize]
    public class PropertyRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PropertyRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: PropertyRequest/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.PropertyTypes = await _context.PropertyTypes.ToListAsync();
            ViewBag.Cities = await _context.Cities.ToListAsync();
            return View();
        }

        // POST: PropertyRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyRequest model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            model.UserId = user.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;

            _context.PropertyRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ تم نشر طلبك بنجاح!";
            return RedirectToAction("MyRequests");
        }

        // GET: PropertyRequest/MyRequests
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var requests = await _context.PropertyRequests
                .Include(r => r.PropertyType)
                .Include(r => r.City)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: PropertyRequest/All
        public async Task<IActionResult> All()
        {
            var requests = await _context.PropertyRequests
                .Include(r => r.PropertyType)
                .Include(r => r.City)
                .Include(r => r.User)
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // POST: PropertyRequest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "⚠️ يجب تسجيل الدخول أولاً" });
                }

                var request = await _context.PropertyRequests.FindAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "⚠️ الطلب غير موجود" });
                }

                if (request.UserId != user.Id)
                {
                    return Json(new { success = false, message = "⚠️ لا يمكنك حذف طلب ليس ملكك" });
                }

                _context.PropertyRequests.Remove(request);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم حذف الطلب بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ حدث خطأ: " + ex.Message });
            }
        }
        // GET: PropertyRequest/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var request = await _context.PropertyRequests
                .Include(r => r.PropertyType)
                .Include(r => r.City)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            if (request.UserId != user.Id) return Forbid();

            ViewBag.PropertyTypes = await _context.PropertyTypes.ToListAsync();
            ViewBag.Cities = await _context.Cities.ToListAsync();

            return View(request);
        }

        // POST: PropertyRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PropertyRequest model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "⚠️ يجب تسجيل الدخول أولاً" });
                }

                if (id != model.Id)
                {
                    return Json(new { success = false, message = "⚠️ طلب غير صحيح" });
                }

                var request = await _context.PropertyRequests.FindAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "⚠️ الطلب غير موجود" });
                }

                if (request.UserId != user.Id)
                {
                    return Json(new { success = false, message = "⚠️ لا يمكنك تعديل طلب ليس ملكك" });
                }

                // ✅ تحديث البيانات (بدون التحقق ModelState)
                request.Title = model.Title ?? request.Title;
                request.PropertyTypeId = model.PropertyTypeId;
                request.Status = model.Status ?? request.Status;
                request.CityId = model.CityId;
                request.Neighborhood = model.Neighborhood;
                request.MinPrice = model.MinPrice;
                request.MaxPrice = model.MaxPrice;
                request.MinArea = model.MinArea;
                request.MaxArea = model.MaxArea;
                request.MinRooms = model.MinRooms;
                request.MaxRooms = model.MaxRooms;
                request.PhoneNumber = model.PhoneNumber ?? request.PhoneNumber;
                request.Email = model.Email ?? request.Email;
                request.Description = model.Description;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ تم تحديث الطلب بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ حدث خطأ: " + ex.Message });
            }
        }
        // API: GetLatest
        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var requests = await _context.PropertyRequests
                .Include(r => r.PropertyType)
                .Include(r => r.City)
                .Include(r => r.User)
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .Select(r => new
                {
                    r.Title,
                    PropertyType = r.PropertyType != null ? r.PropertyType.NameAr : "",
                    City = r.City != null ? r.City.NameAr : "",
                    UserName = r.User != null ? (r.User.FullName ?? r.User.UserName) : "",
                    r.CreatedAt,
                    r.Status,
                    r.MinPrice,
                    r.MaxPrice,
                    r.MinArea,
                    r.MaxArea,
                    r.Neighborhood,
                    r.Description
                })
                .ToListAsync();

            return Json(requests);
        }
    }
}