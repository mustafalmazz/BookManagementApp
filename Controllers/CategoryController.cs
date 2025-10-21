using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace BookManagementApp.Controllers
{
    public class CategoryController : Controller
    {
        private readonly MyDbContext _context;
        public CategoryController(MyDbContext context)
        {
                _context = context;
        }
        public IActionResult List()
        {
            var list = _context.Categories.ToList();
            return View(list);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]  
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            if (category == null)
            {
                return NotFound();
            }
            _context.Categories.Add(category);
            _context.SaveChanges();
            return View();
        }
    }
}
