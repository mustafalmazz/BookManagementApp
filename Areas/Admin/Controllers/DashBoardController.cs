using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashBoardController : Controller
    {
        private readonly MyDbContext _context;
        public DashBoardController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            var viewModel = new DashBoardViewModel
            {
                Books = _context.Books.Where(u => u.UserId == userId).OrderByDescending(x => x.CreateDate).ToList(),
                Categories = _context.Categories.Where(u => u.UserId == userId).ToList()
            };
            return View(viewModel);
        }
    }
}
