using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace BookManagementApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly MyDbContext _context;
        public BooksController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult List()
        {
            var books = _context.Books.ToList();
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
        public IActionResult Edit(Book model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
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
            _context.SaveChanges();

            return RedirectToAction("List");
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Book model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}
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
