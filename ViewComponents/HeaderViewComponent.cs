using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApp.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly MyDbContext _context;
        public HeaderViewComponent(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var categories = await _context.Categories.ToListAsync();

            if (userId != null)
            {
                categories = await _context.Categories.Where(c=>c.UserId == userId).ToListAsync();
            }
            return View(categories);
        }
    }
}
