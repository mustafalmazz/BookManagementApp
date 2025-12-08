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
        public IActionResult List()
        {
            var userList = _context.Users.ToList();


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
            var roles = new List<string> { "User","SuperAdmin" };
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
    }
}
