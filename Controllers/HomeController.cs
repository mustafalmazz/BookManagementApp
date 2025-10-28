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
