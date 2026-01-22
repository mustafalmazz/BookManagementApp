using System.Diagnostics;
using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq; 
using System.Net.Http;    

namespace BookManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;
        public HomeController(MyDbContext context)
        {
            _context = context;
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
            int pageSize = 12; // Google'dan her seferinde 12 kitap çekelim (Grid yapısına uygun)

            // Eğer arama boşsa boş sayfa döndür
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
                    int startIndex = (page - 1) * pageSize;

                    var url = $"https://www.googleapis.com/books/v1/volumes?q={q}&startIndex={startIndex}&maxResults={pageSize}";

                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var data = JObject.Parse(jsonString);

                        // Toplam sonuç sayısını al (Sayfalama hesaplamak için)
                        totalItems = data["totalItems"]?.Value<int>() ?? 0;

                        // Gelen Kitapları Listeye Çevir
                        var items = data["items"];
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var volume = item["volumeInfo"];

                                var newBook = new Book
                                {
                                    // Google'dan gelen verileri bizim Book modeline eşliyoruz
                                    Name = volume["title"]?.ToString() ?? "İsimsiz Eser",
                                    Author = volume["authors"] != null ? string.Join(", ", volume["authors"]) : "Bilinmeyen Yazar",
                                    Description = volume["description"]?.ToString(),
                                    TotalPages = volume["pageCount"]?.Value<int>() ?? 0,
                                    // Resim linkini HTTPS yapıyoruz
                                    Image = volume["imageLinks"]?["thumbnail"]?.ToString().Replace("http://", "https://")
                                };
                                bookList.Add(newBook);
                            }
                        }
                    }
                }
            }
            catch
            {
            }


            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewData["CurrentSearch"] = q;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalRecords"] = totalItems;

            ViewData["CurrentSort"] = "";
            ViewData["CurrentPageCount"] = "";

            return View(bookList);
        }

        public async Task<IActionResult> Index(string q, string sortOrder, int? minPage, int? maxPage, int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            int pageSize = 6; // Sayfa başına kitap sayısı

            // 1. Temel Sorgu
            var books = _context.Books.Where(u => u.UserId == userId).AsQueryable();

            // 2. Arama (Search)
            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                books = books.Where(b => b.Name.ToLower().Contains(q) || b.Author.ToLower().Contains(q));
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
            var relatedBooks = _context.Books.Where(a => a.CategoryId == book.CategoryId && a.Name != book.Name && a.UserId == userId).Take(8).ToList();
            if (book == null)
                return NotFound();
            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                RelatedBooks = relatedBooks
            };
            return View(viewModel);
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
