using System.Diagnostics;
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
            ViewBag.Categories = _context.Categories.ToList();
        }
        public IActionResult Index()
        {
            var model = _context.Books.ToList();
            return View(model);
        }
        public IActionResult Details(int? id)
        {
            if (id == null)
                return NotFound();

            var book = _context.Books
                        .Include(b => b.Category) 
                        .FirstOrDefault(b => b.Id == id);

            if (book == null)
                return NotFound();

            return View(book); 
        }
        public IActionResult Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index");
            }

            var books = _context.Books
                .Where(a => a.Name.Contains(q) || a.Author.Contains(q))
                .ToList();

            return View("Index",books);
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
