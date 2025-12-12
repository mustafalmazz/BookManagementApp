using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    public class CategoriesController : Controller
    {
        private readonly MyDbContext _context;
        public CategoriesController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult List()
        {
            var list = _context.Categories.Include(c=>c.User).OrderByDescending(c => c.Id).ToList();
            return View(list);
        }
    }
}
