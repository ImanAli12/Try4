using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Data;
using RealEstateWebApp.Models;
using System.Security.Claims;

namespace RealEstateWebApp.Controllers
{
    // [Authorize]
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

        [HttpGet]
        [Route("Properties/Create")]
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
            if (user == null)
            {
                return Unauthorized();
            }

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
                var mainImagePath = await SaveImageAsync(viewModel.MainImage, "aqar");
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = "/image/aqar/" + mainImagePath,
                    IsMain = true,
                    Order = 0
                });
            }

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
            return RedirectToAction("MyProperties", "Dashboard");
        }

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

            var user = await _userManager.GetUserAsync(User);
            if (property.AdvertiserId != user?.Id)
            {
                return Forbid();
            }

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

            var city = await _context.Cities.FindAsync(viewModel.CityId);
            if (city == null)
            {
                ModelState.AddModelError("CityId", "المدينة المحددة غير موجودة");
                viewModel.PropertyTypes = await _context.PropertyTypes.ToListAsync();
                viewModel.Cities = await _context.Cities.ToListAsync();
                viewModel.Features = await _context.Features.ToListAsync();
                return View(viewModel);
            }

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

            if (viewModel.MainImage != null && viewModel.MainImage.Length > 0)
            {
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

            if (viewModel.AdditionalImages != null && viewModel.AdditionalImages.Any())
            {
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

            TempData["Success"] = "تم تحديث العقار بنجاح!";
            return RedirectToAction("Index", "Dashboard");
        }

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

            foreach (var image in property.Images)
            {
                DeleteImage(image.ImageUrl);
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حذف العقار بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _context.Cities
                .Select(c => new { c.Id, c.NameAr })
                .ToListAsync();
            return Json(cities);
        }

        [HttpGet]
        public async Task<IActionResult> GetPropertyTypes()
        {
            var types = await _context.PropertyTypes
                .Select(t => new { t.Id, t.NameAr })
                .ToListAsync();
            return Json(types);
        }

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

        // ==========================================================
        // ✅ دالة Details المكتملة (مع جلب العقارات المشابهة)
        // ==========================================================
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

            // ==========================================================
            // ✅ جلب العقارات المشابهة من جدول SimilarProperties
            // ==========================================================
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

            // ✅ ترتيب النتائج حسب RankOrder
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