using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Net.Http; // EKLENDI: Google'a istek atmak için
using Newtonsoft.Json.Linq; // EKLENDI: Gelen JSON verisini okumak için

namespace BookManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BooksController : Controller
    {
        private readonly MyDbContext _context;
        private readonly Account _cloudinaryAccount;
        private readonly Cloudinary _cloudinary;

        public BooksController(MyDbContext context)
        {
            _context = context;

            // --- CLOUDINARY AYARLARI ---
            _cloudinaryAccount = new Account(
                "dpsk6vfqc",          // Cloud Name
                "525372195651899",    // API Key
                "sqznLclKH3nVMqZdArkUsaijG40"  // API Secret
            );
            _cloudinary = new Cloudinary(_cloudinaryAccount);
        }
        [HttpPost]
        public async Task<IActionResult> QuickCreate(Book model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Hızlı eklemede validasyon temizliği
            ModelState.Remove("UserId");
            ModelState.Remove("ImageFile");
            ModelState.Remove("User");
            ModelState.Remove("Category");

            // --- AYNI KİTAP KONTROLÜ (DUPLICATE CHECK) ---
            // Kullanıcının kütüphanesinde aynı isim ve yazara sahip kitap var mı?
            bool isDuplicate = _context.Books.Any(b => b.UserId == userId && b.Name == model.Name && b.Author == model.Author);

            if (isDuplicate)
            {
                // Hata mesajını TempData'ya atıyoruz (Kırmızı uyarı için)
                TempData["ErrorMessage"] = "Bu kitap kütüphanenizde zaten mevcut!";

                // İşlemi iptal edip kullanıcıyı geri gönderiyoruz
                return RedirectToAction("GoogleBooks", "Home", new { area = "", q = model.Name });
            }
            // ---------------------------------------------

            model.UserId = userId.Value;
            model.CreateDate = DateTime.Now;

            // --- AÇIKLAMA KISALTMA (TRUNCATE) ---
            int maxDescLength = 1550;
            if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > maxDescLength)
            {
                model.Description = model.Description.Substring(0, maxDescLength - 3) + "...";
            }

            // Kategori Kontrolü
            var firstCategory = _context.Categories.FirstOrDefault(c => c.UserId == userId);

            if (firstCategory != null)
            {
                model.CategoryId = firstCategory.Id;
            }
            else
            {
                var newCategory = new Category
                {
                    CategoryName = "Google Kitaplar",
                    UserId = userId.Value
                };
                _context.Categories.Add(newCategory);
                await _context.SaveChangesAsync();
                model.CategoryId = newCategory.Id;
            }

            // Resim Kontrolü
            if (string.IsNullOrEmpty(model.Image))
            {
                model.Image = "/images/ResimBulunamadi.png";
            }

            _context.Books.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Kitap başarıyla kütüphanenize eklendi!";

            return RedirectToAction("GoogleBooks", "Home", new { area = "", q = model.Name });
        }
        // --- YENİ EKLENEN METOD: GOOGLE KİTAP ARAMA (API) ---
        [HttpGet]
        public async Task<IActionResult> GetBookFromGoogle(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(new { success = false });

            try
            {
                using (var client = new HttpClient())
                {
                    // Google Books API'ye istek atıyoruz
                    var url = $"https://www.googleapis.com/books/v1/volumes?q={query}&maxResults=1";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var data = JObject.Parse(jsonString);

                        if (data["totalItems"]?.Value<int>() > 0)
                        {
                            var volumeInfo = data["items"]?[0]?["volumeInfo"];

                            // Verileri güvenli bir şekilde çekiyoruz
                            var bookInfo = new
                            {
                                success = true,
                                title = volumeInfo?["title"]?.ToString(),
                                authors = volumeInfo?["authors"] != null ? string.Join(", ", volumeInfo["authors"]) : "",
                                description = volumeInfo?["description"]?.ToString(),
                                pageCount = volumeInfo?["pageCount"]?.ToString(),
                                // Resim linki varsa al ve HTTPS yap (Güvenlik için)
                                imageUrl = volumeInfo?["imageLinks"]?["thumbnail"]?.ToString().Replace("http://", "https://")
                            };
                            return Json(bookInfo);
                        }
                    }
                }
            }
            catch
            {
                // Hata olursa sessizce başarısız dön
                return Json(new { success = false });
            }
            return Json(new { success = false });
        }
        // ------------------------------------------------

        // Parametre olarak sayfa numarası (page) ve arama kelimesi (q) alıyoruz.
        public IActionResult List(int page = 1, string q = "")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 1. Sayfa başına kaç kitap gösterilecek?
            int pageSize = 10;

            // 2. Temel Sorgu (Henüz veritabanına gitmedi, IQueryable olarak bekliyor)
            var query = _context.Books
                .Where(b => b.UserId == userId)
                .Include(b => b.Category)
                .AsQueryable();

            // 3. Eğer arama yapılmışsa filtrele
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(a => a.Name.Contains(q) || a.Author.Contains(q));
            }

            // 4. Toplam Kayıt Sayısını Bul (Sayfalama hesabı için gerekli)
            int totalRecords = query.Count();

            // 5. Toplam Sayfa Sayısını Hesapla (Örn: 25 kitap varsa, 25/10 = 2.5 -> 3 sayfa)
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // 6. Doğru Veriyi Çek (Skip ve Take ile)
            // Örn: 2. sayfadaysak: (2-1)*10 = 10 tane atla, sonraki 10 taneyi al.
            var books = query
                .OrderByDescending(b => b.Id) // Yeniler en üstte olsun diye (İsteğe bağlı)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 7. View Tarafına Gerekli Bilgileri Gönder
            ViewBag.CurrentPage = page;       // Şu an hangi sayfadayız?
            ViewBag.TotalPages = totalPages;  // Toplam kaç sayfa var?
            ViewBag.SearchQuery = q;          // Arama kutusunda yazı kalsın diye

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
        public async Task<IActionResult> Edit(Book model, IFormFile ImageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "" });

            ModelState.Remove("UserId");
            ModelState.Remove("Image");
            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                return View(model);
            }

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == model.Id && b.UserId == userId);
            if (book == null) return NotFound();

            // --- CLOUDINARY RESİM GÜNCELLEME ---
            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(ImageFile.FileName, ImageFile.OpenReadStream()),
                        Transformation = new Transformation().Width(800).Crop("limit")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error != null)
                    {
                        throw new Exception(uploadResult.Error.Message);
                    }

                    book.Image = uploadResult.SecureUrl.ToString();
                }
                catch (Exception)
                {
                    ModelState.AddModelError("Image", "Resim güncellenirken hata oluştu (Cloudinary).");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }
            }

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
            if (userId == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Validasyon Temizlikleri
            ModelState.Remove("UserId");
            ModelState.Remove("Image");     // Google'dan gelen link olabilir, validasyona takılmasın
            ModelState.Remove("ImageFile");
            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                return View(model);
            }

            // 1. Kullanıcı kendi bilgisayarından resim seçtiyse -> Cloudinary'ye yükle
            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(ImageFile.FileName, ImageFile.OpenReadStream()),
                        Transformation = new Transformation().Width(800).Crop("limit")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error != null)
                    {
                        throw new Exception(uploadResult.Error.Message);
                    }

                    model.Image = uploadResult.SecureUrl.ToString();
                }
                catch (Exception)
                {
                    ModelState.AddModelError("Image", "Resim yüklenirken bir hata oluştu (Cloudinary).");
                    ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "CategoryName");
                    return View(model);
                }
            }
            // 2. Eğer dosya seçmediyse ama Google'dan link geldiyse (model.Image doluysa)
            // Hiçbir şey yapmamıza gerek yok, model.Image zaten formdan gelen linki tutuyor.

            model.UserId = userId.Value;
            model.CreateDate = DateTime.Now;

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
            var book = _context.Books.Include(c => c.Category).FirstOrDefault(b => b.Id == id && b.UserId == userId);

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