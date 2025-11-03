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
            var books = _context.Books.Include(a=>a.Category).ToList();
            return View(books);
        }

        public IActionResult Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("List");
            }

            var books = _context.Books
                .Where(a => a.Name.Contains(q) || a.Author.Contains(q))
                .ToList();

            return View("List", books);
        }

        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }
        public IActionResult Edit(int? id)
        {
            ViewBag.Categories = new SelectList(_context.Categories,"Id","CategoryName");
            if (id == null)
            {
                return NotFound();
            }
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }
        [HttpPost]
        public IActionResult Edit(Book model,IFormFile ImageFile)
        {
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
            var book = _context.Books.FirstOrDefault(b => b.Id == model.Id);
            if (book == null)
            {
                return NotFound();
            }
            book.Author = model.Author;
            book.Name = model.Name;
            book.Description = model.Description;
            book.Price = model.Price;
            book.Stock = model.Stock;
            book.CategoryId = model.CategoryId;
            book.Image = model.Image;
            book.TotalPages = model.TotalPages;
            book.Rate = model.Rate;
            _context.SaveChanges();

            return RedirectToAction("List");
        }
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories,"Id","CategoryName");
            return View();
        }
        [HttpPost]
        public IActionResult Create(Book model,IFormFile ImageFile)
        {
            //if (!ModelState.IsValid)
            //{
            //    ViewBag.Categories = new SelectList(_context.Categories, "Id", "CategoryName");
            //    return View(model);
            //}
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
            _context.Books.Add(model);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var forremove = _context.Books.FirstOrDefault(b => b.Id == id);
            if (forremove == null)
            {
                return NotFound();
            }
            _context.Books.Remove(forremove);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
