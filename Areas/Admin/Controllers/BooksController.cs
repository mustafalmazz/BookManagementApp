using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing; 
using SixLabors.ImageSharp.Formats.Jpeg;

namespace BookManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BooksController : Controller
    {
        private readonly MyDbContext _context;

        public BooksController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult List()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var books = _context.Books
                .Where(b => b.UserId == userId)
                .Include(b => b.Category)
                .ToList();

            return View(books);
        }

 
        public IActionResult Search(string q)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("List");
            }

            var books = _context.Books
                .Where(a =>
                    a.UserId == userId &&
                    (a.Name.Contains(q) || a.Author.Contains(q)))
                .Include(a => a.Category)
                .ToList();

            return View("List", books);
        }


        public IActionResult Details(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var book = _context.Books
                .Include(b => b.Category)
                .FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }


        public IActionResult Edit(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var book = _context.Books.FirstOrDefault(b => b.Id == id && b.UserId == userId);
            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => c.UserId == userId),
                "Id",
                "CategoryName",
                book.CategoryId
            );

            return View(book);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Book model, IFormFile ImageFile) // async Task yaptık
        {
            // 1. Yetki Kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 2. Validasyon Temizliği
            ModelState.Remove("UserId");
            ModelState.Remove("Image");
            ModelState.Remove("ImageFile");

            // 3. Validation Kontrolü
            if (!ModelState.IsValid)
            {
                // Hata varsa kategorileri tekrar doldur
                ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                return View(model);
            }

            // 4. Veritabanından Kitabı Bulma
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == model.Id && b.UserId == userId);
            if (book == null)
            {
                return NotFound();
            }

            // 5. RESİM GÜNCELLEME İŞLEMİ (ImageSharp ile Optimize Edildi)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // A. Uzantı Kontrolü
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Image", "Sadece resim dosyaları yüklenebilir.");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }

                try
                {
                    // B. Eski Resmi Silme (Sunucuda çöp dosya birikmesin diye)
                    if (!string.IsNullOrEmpty(book.Image))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // C. Yeni Resim İçin Benzersiz İsim
                    var uniqueFileName = Guid.NewGuid().ToString() + extension;
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    // D. Resize ve Sıkıştırma İşlemi
                    using (var image = await Image.LoadAsync(ImageFile.OpenReadStream()))
                    {
                        // Genişlik 800px'den büyükse küçült
                        if (image.Width > 800)
                        {
                            image.Mutate(x => x.Resize(800, 0));
                        }

                        var encoder = new JpegEncoder
                        {
                            Quality = 75 // %75 Kalite ile kaydet
                        };

                        await image.SaveAsync(filePath, encoder);
                    }

                    // Veritabanı yolunu güncelle
                    book.Image = "/images/" + uniqueFileName;
                }
                catch (Exception)
                {
                    // Hata olursa (Loglanabilir)
                    ModelState.AddModelError("Image", "Resim yüklenirken hata oluştu.");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }
            }
            // Resim yüklenmediyse 'book.Image' değişmez, eski resim kalır.

            // 6. Diğer Bilgilerin Güncellenmesi
            book.Author = model.Author;
            book.Name = model.Name;
            book.Description = model.Description;
            book.Price = model.Price;
            book.Stock = model.Stock;
            book.CategoryId = model.CategoryId;
            book.TotalPages = model.TotalPages;
            book.Rate = model.Rate;
            book.Notes = model.Notes;

            await _context.SaveChangesAsync();
            return RedirectToAction("List");
        }

        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => c.UserId == userId),
                "Id",
                "CategoryName"
            );

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(Book model, IFormFile ImageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // --- VALIDATION TEMİZLİĞİ (Çok Önemli) ---
            // 1. UserId session'dan geliyor, formdan değil.
            ModelState.Remove("UserId");
            // 2. Resim yolu kodla oluşturulacak.
            ModelState.Remove("Image");
            // 3. Dosya nesnesi validasyona dahil değil.
            ModelState.Remove("ImageFile");

            // 4. Navigation Property'ler (User ve Category nesneleri) formdan gelmez.
            // Sadece Id'leri gelir. Bu yüzden nesnelerin kendisini validasyondan çıkarıyoruz.
            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
            {
                // Hata varsa kategorileri TEKRAR doldurmak zorundayız.
                // Yoksa sayfa yenilendiğinde dropdown boş gelir ve hata devam eder.
                ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                return View(model);
            }

            // --- RESİM YÜKLEME İŞLEMLERİ ---
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Image", "Sadece .jpg, .jpeg veya .png yükleyebilirsiniz.");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }

                try
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + extension;
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var image = await Image.LoadAsync(ImageFile.OpenReadStream()))
                    {
                        if (image.Width > 800)
                        {
                            image.Mutate(x => x.Resize(800, 0));
                        }
                        var encoder = new JpegEncoder { Quality = 75 };
                        await image.SaveAsync(filePath, encoder);
                    }

                    model.Image = "/images/" + uniqueFileName;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("Image", "Resim yüklenirken bir hata oluştu.");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }
            }

            model.UserId = userId.Value;
            model.CreateDate = DateTime.Now; // Tarihi de garantiye alalım

            _context.Books.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("List");
        }
        public IActionResult Delete(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var book = _context.Books.Include(c => c.Category).FirstOrDefault(b => b.Id == id && b.UserId == userId);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult DeleteConfirm(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (id == null)
            {
                return NotFound();
            }
            var book = _context.Books.Include(c=>c.Category).FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (book == null)
            {
                return NotFound();
            }
            _context.Books.Remove(book);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
