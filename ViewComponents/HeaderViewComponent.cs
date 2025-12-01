using BookManagementApp.Areas.Admin.Models;
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
            var viewModel = new CategoryUserViewModel();
            if (userId != null)
            {
                viewModel.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                viewModel.Categories = await _context.Categories.Where(c=>c.UserId == userId).ToListAsync();
            }
            return View(viewModel);
        }
    }
}
