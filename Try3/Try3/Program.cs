using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
            using Microsoft.AspNetCore.Identity;
            using Microsoft.EntityFrameworkCore;
            using RealEstateWebApp.Data;
            using RealEstateWebApp.Models;

            var builder = WebApplication.CreateBuilder(args);

            // إضافة الخدمات
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // ربط قاعدة البيانات
            builder.Services.AddDbContext<ApplicationDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Try4")));

            // ✅ إضافة Identity مع دعم Entity Framework
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // إعدادات Cookie الخاصة بـ Identity
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";   // مسار تسجيل الدخول (اختياري)
                options.LogoutPath = "/Account/Logout"; // مسار تسجيل الخروج
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30); // مدة بقاء الجلسة
            });

            var app = builder.Build();

            // ============================================================
            // ✅ تهيئة البيانات (Seed Data)
            // ============================================================
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await SeedData.InitializeAsync(services);
                    Console.WriteLine("✅ تم تهيئة البيانات بنجاح!");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "❌ حدث خطأ أثناء تهيئة البيانات: {Message}", ex.Message);
                }
            }
            // ============================================================

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();   // 🔐 إضافة Authentication
            app.UseAuthorization();    // 🔐 إضافة Authorization

            // Map controller routes if you use controllers:
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map Razor Pages
            app.MapRazorPages();

            app.Run();