using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    public class ContactController : Controller
    {
        private readonly MyDbContext _context;
        public ContactController(MyDbContext context)
        {
                _context = context;
        }
        public async Task<IActionResult> Index()
        {   
            var info = await _context.Contacts.Include(u=>u.User).ToListAsync();
            return View(info);
        }
    }
}
