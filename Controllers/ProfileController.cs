using Microsoft.AspNetCore.Mvc;
using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet; // EKLENDI
using CloudinaryDotNet.Actions; // EKLENDI

namespace BookManagementApp.Controllers
{
    public class ProfileController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _configuration; // Cloudinary ayarlarını okumak için

        public ProfileController(MyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpPost]
        public IActionResult UpdateGoal([FromForm] int goal)
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
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            var userBooks = _context.Books
                                    .Include(b => b.Category)
                                    .Where(b => b.UserId == userId)
                                    .ToList();

            if (user == null) return NotFound();

            // İstatistikler
            var favCategoryGroup = userBooks
                                    .GroupBy(b => b.Category != null ? b.Category.CategoryName : "Diğer")
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault();

            string favCategoryName = favCategoryGroup != null ? favCategoryGroup.Key : "Henüz Yok";

            var model = new UserProfileViewModel
            {
                UserName = user.UserName,
                Role = user.Role ?? "Üye",
                JoinDate = user.CreateDate,

                // Resim yolu (Cloudinary URL'i veya hazır avatar URL'i)
                ProfileImageUrl = user.ProfileImageUrl,

                TotalBooks = userBooks.Count,
                TotalCategories = userBooks.Select(b => b.CategoryId).Distinct().Count(),
                TotalPagesRead = (int)userBooks.Sum(b => b.TotalPages ?? 0),
                TotalMoneySpent = userBooks.Sum(b => b.Price ?? 0),
                FavoriteCategory = favCategoryName,
                BooksReadThisYear = userBooks.Count(b => b.CreateDate.Year == DateTime.Now.Year),
                YearlyReadingGoal = user.YearlyReadingGoal
            };

            return View(model);
        }

        // --- CLOUDINARY İLE GÜNCELLENEN METOT ---
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile file, string avatarUrl)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            try
            {
                // SENARYO 1: Dosya Yükleme (Cloudinary'ye Gönder)
                if (file != null && file.Length > 0)
                {
                    // 1. Cloudinary Ayarlarını Al
                    string cloudName = _configuration["CloudinarySettings:CloudName"];
                    string apiKey = _configuration["CloudinarySettings:ApiKey"];
                    string apiSecret = _configuration["CloudinarySettings:ApiSecret"];

                    // 2. Cloudinary Hesabını Oluştur
                    Account account = new Account(cloudName, apiKey, apiSecret);
                    Cloudinary cloudinary = new Cloudinary(account);

                    // 3. Dosyayı Stream'e Çevir ve Yükleme Parametrelerini Hazırla
                    using (var stream = file.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(file.FileName, stream),
                            // ÖNEMLİ: Avatar için yüz odaklı kare kırpma (Opsiyonel ama önerilir)
                            Transformation = new Transformation().Width(150).Height(150).Crop("fill").Gravity("face")
                        };

                        // 4. Yükle
                        var uploadResult = await cloudinary.UploadAsync(uploadParams);

                        if (uploadResult.Error != null)
                        {
                            return Json(new { success = false, message = "Cloudinary hatası: " + uploadResult.Error.Message });
                        }

                        // 5. Gelen güvenli (HTTPS) URL'i kaydet
                        user.ProfileImageUrl = uploadResult.SecureUrl.AbsoluteUri;
                    }
                }
                // SENARYO 2: Hazır Avatar Seçimi (Direkt URL Kaydet)
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