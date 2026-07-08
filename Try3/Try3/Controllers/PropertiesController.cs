using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropertiesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================
        // CREATE
        // ============================================================
        public async Task<IActionResult> Create()
        {
            var viewModel = new PropertyViewModel
            {
                PropertyTypes = await _context.PropertyTypes.ToListAsync(),
                Cities = await _context.Cities.ToListAsync(),
                Features = await _context.Features.ToListAsync(),
                Status = "للبيع",
                PriceCurrency = "USD",
                AvailableFrom = DateTime.Now
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var city = await _context.Cities.FindAsync(viewModel.CityId);
            if (city == null)
            {
                ModelState.AddModelError("CityId", "المدينة المحددة غير موجودة");
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            var property = new Property
            {
                Code = GeneratePropertyCode(),
                Title = viewModel.Title,
                Price = viewModel.Price,
                PriceCurrency = viewModel.PriceCurrency,
                Area = viewModel.Area,
                Rooms = viewModel.Rooms ?? 0,
                Bathrooms = viewModel.Bathrooms ?? 0,
                Floor = viewModel.Floor,
                Status = viewModel.Status,
                CityId = viewModel.CityId,
                City = city,
                Neighborhood = viewModel.Neighborhood ?? string.Empty,
                Address = viewModel.Address,
                Description = viewModel.Description,
                Latitude = viewModel.Latitude,
                Longitude = viewModel.Longitude,
                PropertyTypeId = viewModel.PropertyTypeId,
                AdvertiserId = user.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ViewsCount = 0,
                DetailedLocation = viewModel.DetailedLocation,
                AvailableFrom = viewModel.AvailableFrom,
                AdvertiserPhone = viewModel.AdvertiserPhone
            };

            if (viewModel.MainImage != null && viewModel.MainImage.Length > 0)
            {
                var path = await SaveImageAsync(viewModel.MainImage, "aqar");
                property.Images.Add(new PropertyImage { ImageUrl = "/image/aqar/" + path, IsMain = true, Order = 0 });
            }

            if (viewModel.AdditionalImages != null)
            {
                int order = 1;
                foreach (var img in viewModel.AdditionalImages)
                {
                    if (img.Length > 0)
                    {
                        var path = await SaveImageAsync(img, "aqar");
                        property.Images.Add(new PropertyImage { ImageUrl = "/image/aqar/" + path, IsMain = false, Order = order++ });
                    }
                }
            }

            if (viewModel.FeatureIds != null && viewModel.FeatureIds.Any())
            {
                var features = await _context.Features.Where(f => viewModel.FeatureIds.Contains(f.Id)).ToListAsync();
                foreach (var f in features) property.Features.Add(f);
            }

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ تم نشر العقار بنجاح!";
            return RedirectToAction("MyProperties", "Properties");
        }

        // ============================================================
        // MY PROPERTIES (عقاراتي)
        // ============================================================
        public async Task<IActionResult> MyProperties()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var properties = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Where(p => p.AdvertiserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(properties);
        }

        // ============================================================
        // EDIT (GET)
        // ============================================================
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Features)
                .Include(p => p.City)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id) return Forbid();

            int propertyNumber = 0;
            if (!string.IsNullOrEmpty(property.Code))
            {
                var parts = property.Code.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int num)) propertyNumber = num;
            }

            var viewModel = new PropertyViewModel
            {
                PropertyTypeId = property.PropertyTypeId,
                Status = property.Status,
                Price = property.Price,
                PriceCurrency = property.PriceCurrency,
                Title = property.Title,
                Description = property.Description,
                CityId = property.CityId,
                Neighborhood = property.Neighborhood ?? string.Empty,
                Address = property.Address,
                DetailedLocation = property.DetailedLocation ?? "",
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                PropertyNumber = propertyNumber,
                Area = property.Area,
                Rooms = property.Rooms,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                TotalFloors = 0,
                AvailableFrom = property.AvailableFrom ?? DateTime.Now,
                AdvertiserPhone = property.AdvertiserPhone ?? "",
                MainImageUrl = property.Images?.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                AdditionalImageUrls = property.Images?.Where(i => !i.IsMain).Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                FeatureIds = property.Features?.Select(f => f.Id).ToList() ?? new List<int>(),
                PropertyTypes = await _context.PropertyTypes.ToListAsync(),
                Cities = await _context.Cities.ToListAsync(),
                Features = await _context.Features.ToListAsync()
            };

            return View(viewModel);
        }

        // ============================================================
        // EDIT (POST) - حفظ التعديلات
        // ============================================================
        // POST: Properties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PropertyViewModel viewModel)
        {
            // ============================================================
            // ✅ التحقق 1: المستخدم مسجل دخول؟
            // ============================================================
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "⚠️ يجب تسجيل الدخول أولاً لتعديل العقار!";
                return RedirectToAction("Login", "Account");
            }

            // ============================================================
            // ✅ التحقق 2: صحة النموذج (جميع الحقول مملوءة؟)
            // ============================================================
            if (!ModelState.IsValid)
            {
                // عرض رسالة عامة
                TempData["Error"] = "⚠️ الرجاء تعبئة جميع الحقول المطلوبة بشكل صحيح!";

                // إعادة تحميل البيانات للـ View
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            // ============================================================
            // ✅ جلب العقار من قاعدة البيانات
            // ============================================================
            var property = await _context.Properties
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                TempData["Error"] = "⚠️ العقار غير موجود!";
                return RedirectToAction("MyProperties", "Properties");
            }

            // ============================================================
            // ✅ التحقق 3: المستخدم هو صاحب العقار؟
            // ============================================================
            if (property.AdvertiserId != user.Id)
            {
                TempData["Error"] = "⚠️ لا يمكنك تعديل هذا العقار لأنه ليس ملكك!";
                return RedirectToAction("MyProperties", "Properties");
            }

            // ============================================================
            // ✅ تحديث البيانات
            // ============================================================
            property.Title = viewModel.Title;
            property.Price = viewModel.Price;
            property.PriceCurrency = viewModel.PriceCurrency;
            property.Area = viewModel.Area;
            property.Rooms = viewModel.Rooms ?? 0;
            property.Bathrooms = viewModel.Bathrooms ?? 0;
            property.Floor = viewModel.Floor;
            property.Status = viewModel.Status;
            property.CityId = viewModel.CityId;
            property.Neighborhood = viewModel.Neighborhood ?? string.Empty;
            property.Address = viewModel.Address;
            property.Description = viewModel.Description;
            property.PropertyTypeId = viewModel.PropertyTypeId;
            property.DetailedLocation = viewModel.DetailedLocation;
            property.AvailableFrom = viewModel.AvailableFrom;
            property.AdvertiserPhone = viewModel.AdvertiserPhone;
            property.Latitude = viewModel.Latitude;
            property.Longitude = viewModel.Longitude;

            // ============================================================
            // ✅ تحديث المرافق
            // ============================================================
            property.Features.Clear();
            if (viewModel.FeatureIds != null && viewModel.FeatureIds.Any())
            {
                var features = await _context.Features
                    .Where(f => viewModel.FeatureIds.Contains(f.Id))
                    .ToListAsync();
                foreach (var feature in features)
                {
                    property.Features.Add(feature);
                }
            }

            // ============================================================
            // ✅ حفظ التغييرات
            // ============================================================
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ تم تحديث العقار بنجاح!";
            return RedirectToAction("MyProperties", "Properties");
        }
        // ============================================================
        // DETAILS (تفاصيل العقار)
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Features)
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Advertiser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id) return Forbid();

            return View(property);
        }

        // ============================================================
        // DELETE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id) return Forbid();

            foreach (var img in property.Images) DeleteImage(img.ImageUrl);

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حذف العقار بنجاح" });
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================
        private async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "image", folder);
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            string fileName = Path.GetFileName(imageUrl);
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "image", "aqar", fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private string GeneratePropertyCode()
        {
            var lastProperty = _context.Properties.OrderByDescending(p => p.Id).FirstOrDefault();
            int nextNumber = (lastProperty?.Id ?? 0) + 1;
            return $"PROP-{DateTime.Now.Year}-{nextNumber:D6}";
        }
    }
}