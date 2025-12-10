using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    //[Authorize(Roles = "SuperAdmin")]
    public class DashBoardController : Controller
    {
        private readonly MyDbContext _context;
        public DashBoardController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var model = new DashBoardViewModel
            {
                Books = _context.Books.ToList(),
                Categories = _context.Categories.ToList(),
                Users = _context.Users.ToList()
            };
            return View(model);
        }
    }
}
