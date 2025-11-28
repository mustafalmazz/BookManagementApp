using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net; 

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

            if (model != null)
            {
                model.PasswordHash = string.Empty;
            }

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

            try
            {
                user.UserName = model.UserName;

                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Hesap bilgileriniz başarıyla güncellendi ✔";

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Güncelleme sırasında beklenmeyen bir hata oluştu.");
                return View(model);
            }
        }

        public IActionResult FixPasswordHash()
        {
            int userIdToFix = 1;
            string currentPlainTextPassword = "123";

            var user = _context.Users.FirstOrDefault(u => u.Id == userIdToFix);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(currentPlainTextPassword);

            user.PasswordHash = newHashedPassword;
            _context.SaveChanges();

            return Content($"Kullanıcı ID: {userIdToFix} için gerçek HASH oluşturuldu ve kaydedildi. " +
                           $"Yeni hash: {newHashedPassword.Substring(0, 20)}...");
        }
    }
}