using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using System.Security.Claims;

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

        // GET: Properties/Create
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

        // POST: Properties/Create
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
            if (user == null)
            {
                return Unauthorized();
            }

            // العثور على المدينة المحددة
            var city = await _context.Cities.FindAsync(viewModel.CityId);
            if (city == null)
            {
                ModelState.AddModelError("CityId", "المدينة المحددة غير موجودة");
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            // إنشاء العقار
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

            // معالجة الصورة الأساسية
            if (viewModel.MainImage != null && viewModel.MainImage.Length > 0)
            {
                var mainImagePath = await SaveImageAsync(viewModel.MainImage, "aqar");
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = "/image/aqar/" + mainImagePath,
                    IsMain = true,
                    Order = 0
                });
            }

            // معالجة الصور الإضافية
            if (viewModel.AdditionalImages != null)
            {
                int order = 1;
                foreach (var image in viewModel.AdditionalImages)
                {
                    if (image.Length > 0)
                    {
                        var imagePath = await SaveImageAsync(image, "aqar");
                        property.Images.Add(new PropertyImage
                        {
                            ImageUrl = "/image/aqar/" + imagePath,
                            IsMain = false,
                            Order = order++
                        });
                    }
                }
            }

            // إضافة المرافق
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

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();


            TempData["Success"] = "✅ تم نشر العقار بنجاح!";
            return RedirectToAction("MyProperties", "Properties");
        }

        // GET: Properties/MyProperties
        public async Task<IActionResult> MyProperties()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var properties = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Where(p => p.AdvertiserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(properties);
        }

        // GET: Properties/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Features)
                .Include(p => p.City)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            // التحقق من أن المعلن هو صاحب العقار
            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id)
            {
                return Forbid();
            }

            // استخراج رقم المحضر من الكود
            int propertyNumber = 0;
            if (!string.IsNullOrEmpty(property.Code))
            {
                var parts = property.Code.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int num))
                {
                    propertyNumber = num;
                }
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

        // POST: Properties/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PropertyViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id)
            {
                return Forbid();
            }

            // العثور على المدينة المحددة
            var city = await _context.Cities.FindAsync(viewModel.CityId);
            if (city == null)
            {
                ModelState.AddModelError("CityId", "المدينة المحددة غير موجودة");
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

            // تحديث البيانات
            property.Title = viewModel.Title;
            property.Price = viewModel.Price;
            property.PriceCurrency = viewModel.PriceCurrency;
            property.Area = viewModel.Area;
            property.Rooms = viewModel.Rooms ?? 0;
            property.Bathrooms = viewModel.Bathrooms ?? 0;
            property.Floor = viewModel.Floor;
            property.Status = viewModel.Status;
            property.CityId = viewModel.CityId;
            property.City = city;
            property.Neighborhood = viewModel.Neighborhood ?? string.Empty;
            property.Address = viewModel.Address;
            property.Description = viewModel.Description;
            property.Latitude = viewModel.Latitude;
            property.Longitude = viewModel.Longitude;
            property.PropertyTypeId = viewModel.PropertyTypeId;
            property.DetailedLocation = viewModel.DetailedLocation;
            property.AvailableFrom = viewModel.AvailableFrom;
            property.AdvertiserPhone = viewModel.AdvertiserPhone;

            // معالجة الصورة الأساسية الجديدة
            if (viewModel.MainImage != null && viewModel.MainImage.Length > 0)
            {
                // حذف الصورة القديمة
                var oldMain = property.Images.FirstOrDefault(i => i.IsMain);
                if (oldMain != null)
                {
                    DeleteImage(oldMain.ImageUrl);
                    property.Images.Remove(oldMain);
                }

                var mainImagePath = await SaveImageAsync(viewModel.MainImage, "aqar");
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = "/image/aqar/" + mainImagePath,
                    IsMain = true,
                    Order = 0
                });
            }

            // معالجة الصور الإضافية الجديدة
            if (viewModel.AdditionalImages != null && viewModel.AdditionalImages.Any())
            {
                // حذف الصور الإضافية القديمة
                var oldImages = property.Images.Where(i => !i.IsMain).ToList();
                foreach (var oldImg in oldImages)
                {
                    DeleteImage(oldImg.ImageUrl);
                    property.Images.Remove(oldImg);
                }

                int order = 1;
                foreach (var image in viewModel.AdditionalImages)
                {
                    if (image.Length > 0)
                    {
                        var imagePath = await SaveImageAsync(image, "aqar");
                        property.Images.Add(new PropertyImage
                        {
                            ImageUrl = "/image/aqar/" + imagePath,
                            IsMain = false,
                            Order = order++
                        });
                    }
                }
            }

            // تحديث المرافق
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

            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ تم تحديث العقار بنجاح!";
            return RedirectToAction("MyProperties", "Properties");
        }
        // GET: Properties/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .Include(p => p.Features)
                .Include(p => p.City)
                .Include(p => p.PropertyType)
                .Include(p => p.Advertiser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id)
            {
                return Forbid();
            }

            return View(property);
        }

        // POST: Properties/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id)
            {
                return Forbid();
            }

            // حذف الصور من المجلد
            foreach (var image in property.Images)
            {
                DeleteImage(image.ImageUrl);
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حذف العقار بنجاح" });
        }

        // GET: Properties/GetCities
        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _context.Cities
                .Select(c => new { c.Id, c.NameAr })
                .ToListAsync();
            return Json(cities);
        }

        // GET: Properties/GetPropertyTypes
        [HttpGet]
        public async Task<IActionResult> GetPropertyTypes()
        {
            var types = await _context.PropertyTypes
                .Select(t => new { t.Id, t.NameAr })
                .ToListAsync();
            return Json(types);
        }

        // Helper Methods
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
            var lastProperty = _context.Properties
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();

            int nextNumber = (lastProperty?.Id ?? 0) + 1;
            return $"PROP-{DateTime.Now.Year}-{nextNumber:D6}";
        }
       
    }

    }
