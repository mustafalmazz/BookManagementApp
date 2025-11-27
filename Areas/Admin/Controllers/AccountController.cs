using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = _context.Users.FirstOrDefault(u => u.Id == userId);
            model.PasswordHash = string.Empty;
            return View(model);
        }
        [HttpPost]
        public IActionResult Index(User model)
        {
            if (model == null)
            {
                return NotFound();
            }
           var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);
            if (user == null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            user.UserName = model.UserName;
            if (!string.IsNullOrEmpty(model.PasswordHash))
            {
                user.PasswordHash = model.PasswordHash;
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
