using Microsoft.AspNetCore.Mvc;
using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BookManagementApp.Controllers
{
    public class ProfileController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _configuration;

        public ProfileController(MyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // --- 1. YILLIK HEDEF GÜNCELLEME ---
        [HttpPost]
        public IActionResult UpdateYearlyGoal([FromForm] int goal)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı. Lütfen tekrar giriş yapın." });

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.YearlyReadingGoal = goal;
                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Kullanıcı veritabanında bulunamadı." });
        }

        // --- 2. AYLIK HEDEF VE OKUNAN SAYFA GÜNCELLEME (GÜNCELLENDİ) ---
        // Artık hem hedefi hem de okunan sayfa sayısını manuel alıyor.
        [HttpPost]
        public IActionResult UpdateMonthlyGoal([FromForm] int goal, [FromForm] int readPages)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                // Hem hedefi hem de manuel girilen okunan sayfayı kaydediyoruz
                user.MonthlyReadingGoal = goal;
                user.MonthlyPagesRead = readPages;

                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Kullanıcı veritabanında bulunamadı." });
        }

        public IActionResult Index()
        {
            // 1. Oturum Kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            // 2. Kullanıcıyı Çek
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            // 3. Kullanıcının Kitaplarını Çek
            var userBooks = _context.Books
                                    .Include(b => b.Category)
                                    .Where(b => b.UserId == userId)
                                    .ToList();

            // 4. İstatistikler: Favori Kategori Hesaplama
            var favCategoryGroup = userBooks
                                    .GroupBy(b => b.Category != null ? b.Category.CategoryName : "Diğer")
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault();

            string favCategoryName = favCategoryGroup != null ? favCategoryGroup.Key : "Henüz Yok";

            var now = DateTime.Now;

            // =========================================================
            // YENİ EKLENEN KISIM: ÇALIŞMA (POMODORO) İSTATİSTİKLERİ
            // =========================================================

            // Kullanıcının tüm çalışma verilerini veritabanından çek (Listeye çevirki bellekte işlesin)
            var userSessions = _context.StudySessions
                                       .Where(s => s.UserId == userId)
                                       .ToList();

            // Bugün çalışılan toplam dakika (CreatedAt tarihinin sadece Gün/Ay/Yıl kısmına bakıyoruz)
            int todayStudyMins = userSessions
                .Where(s => s.CreatedAt.Date == now.Date)
                .Sum(s => s.DurationInMinutes);

            // Bu ay çalışılan toplam dakika (Aynı yıl ve aynı ayda olanları topla)
            int monthStudyMins = userSessions
                .Where(s => s.CreatedAt.Year == now.Year && s.CreatedAt.Month == now.Month)
                .Sum(s => s.DurationInMinutes);

            // Başarıyla tamamlanmış Pomodoro sayısı
            int completedPomodoros = userSessions
                .Count(s => s.SessionType == "Pomodoro" && s.IsCompleted);

            // =========================================================

            // 5. ViewModel'i Doldur
            var model = new UserProfileViewModel
            {
                UserName = user.UserName,
                Role = user.Role ?? "Üye",
                JoinDate = user.CreateDate,
                ProfileImageUrl = user.ProfileImageUrl,

                TotalBooks = userBooks.Count,
                TotalCategories = userBooks.Select(b => b.CategoryId).Distinct().Count(),
                TotalPagesRead = (int)userBooks.Sum(b => b.TotalPages ?? 0),
                TotalMoneySpent = userBooks.Sum(b => b.Price ?? 0),

                FavoriteCategory = favCategoryName,

                BooksReadThisYear = userBooks.Count(b => b.CreateDate.Year == now.Year),
                YearlyReadingGoal = user.YearlyReadingGoal,

                MonthlyReadingGoal = user.MonthlyReadingGoal,
                MonthlyPagesRead = user.MonthlyPagesRead,

                // --- YENİ EKLENEN DEĞERLERİ MODELE AKTARIYORUZ ---
                TodayStudyMinutes = todayStudyMins,
                ThisMonthStudyMinutes = monthStudyMins,
                TotalPomodoroCompleted = completedPomodoros
            };

            return View(model);
        }

        // --- AVATAR GÜNCELLEME ---
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile file, string avatarUrl)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            try
            {
                // SENARYO 1: Dosya Yükleme (Cloudinary)
                if (file != null && file.Length > 0)
                {
                    string cloudName = _configuration["CloudinarySettings:CloudName"];
                    string apiKey = _configuration["CloudinarySettings:ApiKey"];
                    string apiSecret = _configuration["CloudinarySettings:ApiSecret"];

                    Account account = new Account(cloudName, apiKey, apiSecret);
                    Cloudinary cloudinary = new Cloudinary(account);

                    using (var stream = file.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(file.FileName, stream),
                            Transformation = new Transformation().Width(150).Height(150).Crop("fill").Gravity("face")
                        };

                        var uploadResult = await cloudinary.UploadAsync(uploadParams);

                        if (uploadResult.Error != null)
                        {
                            return Json(new { success = false, message = "Cloudinary hatası: " + uploadResult.Error.Message });
                        }

                        user.ProfileImageUrl = uploadResult.SecureUrl.AbsoluteUri;
                    }
                }
                // SENARYO 2: Hazır Avatar Seçimi
                else if (!string.IsNullOrEmpty(avatarUrl))
                {
                    user.ProfileImageUrl = avatarUrl;
                }
                else
                {
                    return Json(new { success = false, message = "Dosya veya avatar seçilmedi." });
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }
    }
}