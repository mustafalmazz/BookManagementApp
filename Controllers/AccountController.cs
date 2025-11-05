using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        public AccountController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {
            
            if (ModelState.IsValid)
            {
                var control = _context.Users.FirstOrDefault(u => u.UserName == user.UserName);
                if (control != null)
                {
                    ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten alınmış!");
                    return View(user);
                }
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }
           return View(user);
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]  
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.UserName);

                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Geçersiz kullanıcı adı veya şifre.";
            return View();  
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}
