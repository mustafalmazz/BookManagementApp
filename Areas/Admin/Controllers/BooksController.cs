using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Edit(Book model, IFormFile ImageFile)
        {
            // 1. Yetki Kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 2. ModelState Temizliği (Hatalı uyarıları engellemek için)
            // UserId formdan gelmiyor, session'dan alıyoruz. Hata vermesin diye siliyoruz.
            ModelState.Remove("UserId");

            // Edit işleminde resim seçmek zorunlu değildir (eskisi kalabilir). 
            // Bu yüzden resim için hata vermesini engelliyoruz.
            ModelState.Remove("Image");
            ModelState.Remove("ImageFile");

            // 3. VALIDATION KONTROLÜ
            if (!ModelState.IsValid)
            {
                // Eğer validation hatası varsa (örn: Puan 6 girilmişse),
                // Dropdown'ın (Kategorilerin) tekrar dolması gerekir, yoksa hata alırız.
                // Burayı kendi kategori çekme kodunuza göre düzenleyin:
                // ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");

                return View(model); // Hataları göstermek için sayfayı geri yükle
            }

            // 4. Veritabanından Kitabı Bulma
            var book = _context.Books.FirstOrDefault(b => b.Id == model.Id && b.UserId == userId);
            if (book == null)
            {
                return NotFound();
            }

            // 5. Resim Güncelleme İşlemi (Varsa)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
                book.Image = "/images/" + fileName; // Sadece yeni resim yüklenirse güncelle
            }
            // Resim yüklenmediyse 'book.Image'a dokunmuyoruz, eski resim kalıyor.

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

            _context.SaveChanges();
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
        public IActionResult Create(Book model, IFormFile ImageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            ModelState.Remove("UserId");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
                model.Image = "/images/" + fileName;
            }

            model.UserId = userId.Value;
            _context.Books.Add(model);
            _context.SaveChanges();

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
