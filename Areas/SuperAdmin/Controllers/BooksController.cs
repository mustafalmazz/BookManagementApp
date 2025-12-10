using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
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
    }
}
