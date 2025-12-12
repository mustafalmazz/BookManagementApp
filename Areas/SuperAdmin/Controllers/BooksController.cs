using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    public class BooksController : Controller
    {
        private readonly MyDbContext _context;
        public BooksController(MyDbContext context)
        {
           _context = context;
        }
        public IActionResult List()
        {
            var books = _context.Books
                                .Include(b => b.Category)
                                .OrderByDescending(b => b.Id) 
                                .ToList();
            return View(books);
        }
        public IActionResult Edit(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            ViewBag.CategoryList = new SelectList(_context.Categories.ToList(), "Id", "Name", book.CategoryId);
            return View(book);
        }

        [HttpPost]
        public IActionResult Edit(Book book)
        {
            ModelState.Remove("Category");
            ModelState.Remove("User"); // User da null gelebilir

            if (!ModelState.IsValid)
            {
                var categories = _context.Categories.ToList();
                if (categories == null || !categories.Any())
                {
                    categories = new List<Category>(); // Boş liste oluştur
                }
                ViewBag.CategoryList = new SelectList(categories, "Id", "Name", book.CategoryId);
                return View(book);
            }

            var existingBook = _context.Books.FirstOrDefault(b => b.Id == book.Id);
            if (existingBook == null)
            {
                return NotFound();
            }

            existingBook.Name = book.Name;
            existingBook.Image = book.Image;
            existingBook.Notes = book.Notes;
            existingBook.Rate = book.Rate;
            existingBook.Description = book.Description;
            existingBook.Author = book.Author;
            existingBook.CategoryId = book.CategoryId;

            _context.SaveChanges();

            return RedirectToAction("List");
        }
        public IActionResult Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("List");
            }
            var books = _context.Books.Include(b => b.Category)
                                .Where(b => b.Name.Contains(q) || b.Author.Contains(q) || b.Category.CategoryName.Contains(q))
                                .OrderByDescending(b => b.Id)
                                .ToList();
            return View("List", books);
        }
    }
}
