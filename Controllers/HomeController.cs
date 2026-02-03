using System.Diagnostics;
using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq; 
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BookManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;
        private readonly Cloudinary _cloudinary;
        public HomeController(MyDbContext context, Cloudinary cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }
        public IActionResult Create()
        {
            return View();
        }
        [AllowAnonymous] 
        public async Task<IActionResult> Landing()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index");
            }

            return View();
        }
        private void LoadCategories()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                ViewBag.Categories = _context.Categories.Where(x => x.UserId == userId).ToList();

            }
            else
            {
                ViewBag.Categories = new List<Category>();
            }

        }
        public async Task<IActionResult> GoogleBooks(string q, int page = 1)
        {
            int pageSize = 12;

            // Arama boşsa boş dön
            if (string.IsNullOrWhiteSpace(q))
            {
                ViewData["CurrentSearch"] = "";
                ViewData["TotalPages"] = 0;
                ViewData["CurrentPage"] = 1;
                ViewData["TotalRecords"] = 0;
                return View(new List<Book>());
            }

            var bookList = new List<Book>();
            int totalItems = 0;

            try
            {
                using (var client = new HttpClient())
                {
                    // 1. User-Agent (Bu kısım yine çok önemli, tarayıcı taklidi yapıyoruz)
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    int startIndex = (page - 1) * pageSize;

                    // --- GÜNCELLENEN KISIM: HEM BAŞLIK HEM YAZAR ARAMASI ---
                    string searchParam = q;

                    // Eğer kullanıcı özel bir filtre (örn: isbn:) kullanmadıysa biz müdahale ediyoruz
                    if (!q.Contains(":"))
                    {
                        // Parantez içinde (intitle:X OR inauthor:X) diyerek iki alana da bakmasını sağlıyoruz.
                        // Bu sayede "Victor Hugo" yazınca da "Sefiller" yazınca da sonuç döner.
                        searchParam = $"(intitle:{q} OR inauthor:{q})";
                    }

                    // Url'e langRestrict=tr eklemeye devam ediyoruz, bu bot filtresini gevşetiyor.
                    var url = $"https://www.googleapis.com/books/v1/volumes?q={searchParam}&startIndex={startIndex}&maxResults={pageSize}&printType=books&langRestrict=tr&orderBy=relevance";

                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var data = JObject.Parse(jsonString);

                        totalItems = data["totalItems"]?.Value<int>() ?? 0;

                        var items = data["items"];
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var volume = item["volumeInfo"];

                                // Null check işlemlerini güvenli hale getirelim
                                string imageLink = volume["imageLinks"]?["thumbnail"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(imageLink))
                                {
                                    imageLink = imageLink.Replace("http://", "https://");
                                }

                                var newBook = new Book
                                {
                                    Name = volume["title"]?.ToString() ?? "İsimsiz Eser",
                                    // Yazar listesi bazen null gelebilir
                                    Author = volume["authors"] != null
                                             ? string.Join(", ", volume["authors"].Select(a => a.ToString()))
                                             : "Bilinmeyen Yazar",
                                    Description = volume["description"]?.ToString(),
                                    TotalPages = volume["pageCount"]?.Value<int>() ?? 0,
                                    Image = imageLink
                                };
                                bookList.Add(newBook);
                            }
                        }
                    }
                    else
                    {
                        // --- 2. DÜZELTME: Hata Durumunu Loglama ---
                        // Eğer Google 403 veya 429 dönerse burada durup hatayı görebilirsin.
                        var errorContent = await response.Content.ReadAsStringAsync();
                        // Buraya Breakpoint koyarak 'errorContent' değişkenini incele.
                        // Google'ın neden reddettiği orada yazar.
                        ViewData["Error"] = $"Google Hatası: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata olduğunda boş dönmek yerine hatayı görmek için breakpoint koymalısın.
                // ex.Message sana internet sorunu mu yoksa kod sorunu mu olduğunu söyler.
                ViewData["Error"] = ex.Message;
            }

            // Sayfalama Hesabı
            int totalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;

            ViewData["CurrentSearch"] = q;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalRecords"] = totalItems;

            // Eksik ViewData'ları doldur (Hata almamak için)
            ViewData["CurrentSort"] = "";
            ViewData["CurrentPageCount"] = "";

            return View(bookList);
        }

        public async Task<IActionResult> Index(string q, string sortOrder, int? minPage, int? maxPage, int? categoryId,int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            int pageSize = 8; // Sayfa başına kitap sayısı

            // 1. Temel Sorgu
            var books = _context.Books.Where(u => u.UserId == userId).AsQueryable();

            // 2. Arama (Search)
            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                books = books.Where(b => b.Name.ToLower().Contains(q) || b.Author.ToLower().Contains(q));
            }
            //2,5
            if (categoryId != null && categoryId > 0)
            {
                books = books.Where(b => b.CategoryId == categoryId);
                ViewData["CurrentCategory"] = categoryId; // Sayfalama yaparken kategori kaybolmasın diye view'a gönderiyoruz
            }

            // 3. Manuel Sayfa Sayısı Filtresi (Min - Max)
            if (minPage != null)
            {
                books = books.Where(b => b.TotalPages >= minPage);
            }

            if (maxPage != null)
            {
                books = books.Where(b => b.TotalPages <= maxPage);
            }

            // 4. Sıralama (Sort) - Güncellendi
            switch (sortOrder)
            {
                case "date_asc": // En Eski
                    books = books.OrderBy(x => x.CreateDate);
                    break;
                case "rate_desc": // Puana Göre (Yüksekten Düşüğe)
                    books = books.OrderByDescending(b => b.Rate);
                    break;
                case "asc": // İsim A-Z
                    books = books.OrderBy(b => b.Name);
                    break;
                case "desc": // İsim Z-A
                    books = books.OrderByDescending(b => b.Name);
                    break;
                case "date_desc": // En Yeni
                default: // Varsayılan
                    books = books.OrderByDescending(x => x.CreateDate);
                    break;
            }

            // --- SAYFALAMA MANTIĞI (PAGINATION) ---

            int totalRecords = await books.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var pagedData = await books
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // View'a verileri gönder (Inputların içi dolu kalsın diye)
            ViewData["CurrentSearch"] = q;
            ViewData["CurrentSort"] = sortOrder;

            // Min ve Max değerlerini geri gönderiyoruz
            ViewData["MinPage"] = minPage;
            ViewData["MaxPage"] = maxPage;

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalRecords"] = totalRecords;

            return View(pagedData);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBookInfo([FromForm] UpdateBookInfoRequest request, IFormFile imageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Oturum süresi dolmuş." });
            }

            try
            {
                var book = _context.Books.FirstOrDefault(b => b.Id == request.BookId && b.UserId == userId);

                if (book == null)
                {
                    return Json(new { success = false, message = "Kitap bulunamadı." });
                }

                // Bilgileri güncelle
                book.Name = request.Name;
                book.Author = request.Author;
                book.CategoryId = request.CategoryId;
                book.TotalPages = request.TotalPages;
                 book.Rate = request.Rating; // Puanlama genelde ayrı metodla yapılıyor ama formdan geliyorsa açabilirsin.

                // -----------------------------------------------------------
                // RESİM YÜKLEME KISMI (CLOUDINARY ENTEGRASYONU)
                // -----------------------------------------------------------
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                            Transformation = new Transformation().Width(800).Crop("limit")
                        };

                        // _cloudinary nesnesinin Constructor'da tanımlı olması gerekir!
                        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                        if (uploadResult.Error != null)
                        {
                            return Json(new { success = false, message = "Cloudinary Hatası: " + uploadResult.Error.Message });
                        }

                        // Resim URL'sini veritabanına kaydet
                        book.Image = uploadResult.SecureUrl.ToString();
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Resim yüklenirken hata oluştu: " + ex.Message });
                    }
                }
                // -----------------------------------------------------------

                _context.SaveChanges();

                return Json(new { success = true, message = "Kitap bilgileri başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        public IActionResult Details(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (id == null)
                return NotFound();

            var book = _context.Books
                        .Include(b => b.Category)
                        .FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (book == null)
                return NotFound();

            var relatedBooks = _context.Books
                .Where(a => a.CategoryId == book.CategoryId && a.Name != book.Name && a.UserId == userId)
                .Take(8)
                .ToList();

            // ⭐ YENİ EKLENEN - Tüm kategorileri getir
            var allCategories = _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CategoryName)
                .ToList();

            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                RelatedBooks = relatedBooks,
                AllCategories = allCategories  // ⭐ YENİ EKLENEN
            };

            return View(viewModel);
        }
        [HttpPost]
        public IActionResult UpdateRating([FromBody] UpdateRatingModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Oturum süresi dolmuş." });
            }

            if (model.Rating < 0.5m || model.Rating > 5)
            {
                return Json(new { success = false, message = "Geçersiz puan değeri." });
            }

            var book = _context.Books.FirstOrDefault(b => b.Id == model.BookId && b.UserId == userId);

            if (book == null)
            {
                return Json(new { success = false, message = "Kitap bulunamadı." });
            }

            book.Rate = model.Rating;
            _context.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult UpdateDescription([FromBody] UpdateDescriptionModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Oturum süresi dolmuş." });
            }

            var book = _context.Books.FirstOrDefault(b => b.Id == model.BookId && b.UserId == userId);

            if (book == null)
            {
                return Json(new { success = false, message = "Kitap bulunamadı." });
            }

            book.Description = model.Description;
            _context.SaveChanges();

            return Json(new { success = true });
        }
        //public IActionResult Search(string q)
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (userId == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }
        //    if (string.IsNullOrWhiteSpace(q))
        //    {
        //        return RedirectToAction("Index");
        //    }

        //    var books = _context.Books
        //        .Where(a => a.UserId == userId && (a.Name.Contains(q) || a.Author.Contains(q)))
        //        .ToList();

        //    return View("Index", books);
        //}
        //public IActionResult List()
        //{
        //    var model = _context.Books.ToList();
        //    return View(model);
        //}
        public IActionResult CategoryList()
        {
            var books = _context.Categories.Include(a => a.Books).ToList();
            return View(books);
        }
        public IActionResult Notes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var book = _context.Books.FirstOrDefault(b => b.UserId == userId && b.Id == id);
            return View(book);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Notes(Book model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingBook = _context.Books.FirstOrDefault(b => b.Id == model.Id && b.UserId == userId);

            if (existingBook == null)
            {
                return NotFound();
            }
            existingBook.Notes = model.Notes;
            _context.SaveChanges();
            return RedirectToAction("Notes", new { id = model.Id });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task<IActionResult> BooksByCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            LoadCategories();

            var books = await _context.Books
                .Include(b => b.Category)
                .Where(b => b.CategoryId == id && b.UserId == userId)
                .ToListAsync();


            var category = await _context.Categories.FindAsync(id);
            ViewData["CategoryName"] = category?.CategoryName ?? "Kategori";

            return View("Index", books);
        }

        public IActionResult SendMeMessage()
        {
            
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult SendMeMessage(Contact contact)
        {
            var lastMessageTime = HttpContext.Session.GetString("LastMessageTime");

            if (!string.IsNullOrEmpty(lastMessageTime))
            {
                var timeDiff = DateTime.Now - DateTime.Parse(lastMessageTime);
                if (timeDiff.TotalSeconds < 60)
                {
                    TempData["ErrorMessage"] = "Çok hızlı işlem yapıyorsunuz. Lütfen yeni mesaj için bir süre bekleyin.";
                    return View(contact); 
                }
            }

            if (contact == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(contact);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                contact.UserId = userId;

                var user = _context.Users.FirstOrDefault(c => c.Id == userId);
                if (user != null)
                {
                    contact.GuestName = user.UserName;
                    contact.GuestEmail = user.Email;
                }
            }

            contact.CreatedDate = DateTime.Now;

            _context.Contacts.Add(contact);
            _context.SaveChanges();

            HttpContext.Session.SetString("LastMessageTime", DateTime.Now.ToString());

            TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi!";
            return RedirectToAction("SendMeMessage");
        }
    }
}
