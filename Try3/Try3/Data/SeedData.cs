using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Models;

namespace RealEstateWebApp.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // إضافة أنواع العقارات
            if (!context.PropertyTypes.Any())
            {
                context.PropertyTypes.AddRange(
                    new PropertyType { NameAr = "شقة" },
                    new PropertyType { NameAr = "فيلا" },
                    new PropertyType { NameAr = "محل تجاري" },
                    new PropertyType { NameAr = "أرض زراعية" },
                    new PropertyType { NameAr = "مكتب" },
                    new PropertyType { NameAr = "مزرعة" },
                    new PropertyType { NameAr = "مستودع" }
                );
            }

            // إضافة المدن
            if (!context.Cities.Any())
            {
                context.Cities.AddRange(
                    new City { NameAr = "دمشق" },
                    new City { NameAr = "حلب" },
                    new City { NameAr = "حمص" },
                    new City { NameAr = "اللاذقية" },
                    new City { NameAr = "طرطوس" },
                    new City { NameAr = "حماة" },
                    new City { NameAr = "الرقة" },
                    new City { NameAr = "دير الزور" },
                    new City { NameAr = "الحسكة" },
                    new City { NameAr = "درعا" },
                    new City { NameAr = "السويداء" },
                    new City { NameAr = "القنيطرة" },
                    new City { NameAr = "إدلب" },
                    new City { NameAr = "ريف دمشق" }
                );
            }

            // إضافة المرافق
            if (!context.Features.Any())
            {
                context.Features.AddRange(
                    new Feature { NameAr = "باحة امامية", IconClass = "fa-door-open" },
                    new Feature { NameAr = "عشب", IconClass = "fa-leaf" },
                    new Feature { NameAr = "إطلالة على البحر", IconClass = "fa-water" },
                    new Feature { NameAr = "موقف سيارات", IconClass = "fa-car" },
                    new Feature { NameAr = "حمام سباحة", IconClass = "fa-person-swimming" },
                    new Feature { NameAr = "مصعد", IconClass = "fa-elevator" },
                    new Feature { NameAr = "تكييف مركزي", IconClass = "fa-snowflake" },
                    new Feature { NameAr = "تدفئة", IconClass = "fa-fire" },
                    new Feature { NameAr = "إنترنت", IconClass = "fa-wifi" },
                    new Feature { NameAr = "كابل TV", IconClass = "fa-tv" },
                    new Feature { NameAr = "حديقة", IconClass = "fa-tree" },
                    new Feature { NameAr = "شرفة", IconClass = "fa-window-maximize" },
                    new Feature { NameAr = "مطبخ مجهز", IconClass = "fa-kitchen-set" },
                    new Feature { NameAr = "مخزن", IconClass = "fa-boxes-stacked" },
                    new Feature { NameAr = "حراسة أمنية", IconClass = "fa-shield" },
                    new Feature { NameAr = "كاميرات مراقبة", IconClass = "fa-video" },
                    new Feature { NameAr = "نادي صحي", IconClass = "fa-dumbbell" },
                    new Feature { NameAr = "ساونا", IconClass = "fa-hot-tub-person" }
                );
            }

            await context.SaveChangesAsync();
        }
    }
}
