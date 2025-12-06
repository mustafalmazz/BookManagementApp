using System.Diagnostics;
using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = _context.Books.Where(u => u.UserId == userId).OrderByDescending(x => x.CreateDate).ToList();
            return View(model);
        }
        public IActionResult Details(int? id)
        {
            if (id == null)
                return NotFound();

            var book = _context.Books
                        .Include(b => b.Category)
                        .FirstOrDefault(b => b.Id == id);
            var relatedBooks = _context.Books.Where(a => a.CategoryId == book.CategoryId && a.Name != book.Name).Take(4).ToList();
            if (book == null)
                return NotFound();
            var viewModel = new BookDetailsViewModel
            {
                Book = book,
                RelatedBooks = relatedBooks
            };
            return View(viewModel);
        }
        public IActionResult Search(string q)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index");
            }

            var books = _context.Books
                .Where(a => a.UserId == userId && (a.Name.Contains(q) || a.Author.Contains(q)))
                .ToList();

            return View("Index", books);
        }
        public IActionResult List()
        {
            var model = _context.Books.ToList();
            return View(model);
        }
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
        public IActionResult SendMeMessage(Contact contact)
        {
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
            }
            var user = _context.Contacts.FirstOrDefault(c=>c.UserId == userId);
            contact.CreatedDate = DateTime.Now;
            contact.GuestName = user?.User?.UserName;
            contact.GuestEmail = user?.User?.Email;
            _context.Contacts.Add(contact);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
