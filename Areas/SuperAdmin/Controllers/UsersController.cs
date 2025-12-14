using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    public class UsersController : Controller
    {
        private readonly MyDbContext _context;
        public UsersController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult Search(string q)
        {
            var users = _context.Users.Where(u => u.UserName.Contains(q) || u.Email.Contains(q)).ToList();
            return View("List", users);
        }
        public IActionResult Add()
        {
            var roles = new List<string> { "User", "SuperAdmin" };
            ViewBag.Roles = new SelectList(roles, "User");
            return View();
        }
        [HttpPost]
        public IActionResult Add(User model)
        {
            if (_context.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten sistemde kayıtlı.");
                return View(model);
            }

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                return View(model);
            }
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                }
                _context.Users.Add(model);
                _context.SaveChanges();
                return RedirectToAction("List");
            }
            return View(model);
        }
        public IActionResult List()
        {
            var userList = _context.Users.Include(u => u.Books).Include(c => c.Categories).ToList();


            return View(userList);
        }
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = _context.Users.Include(u => u.Books).FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = new List<string> { "User", "SuperAdmin" };
            ViewBag.Roles = new SelectList(roles, user.Role); ;
            return View(user);
        }
        [HttpPost]
        public IActionResult Edit(User model)
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
            user.UserName = model.UserName;
            user.Role = model.Role;
            user.Email = model.Email;
            //user.PasswordHash = model.PasswordHash;
            _context.SaveChanges();

            return RedirectToAction("List");
        }
   
        public async Task<IActionResult> DeleteConfirm(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Delete(int id)
        {
            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null)
            {
                return NotFound();
            }

            _context.Users.Remove(existingUser);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(List));
        }
    }
}
