using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BookManagementApp.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly MyDbContext _context;

        public HeaderViewComponent(MyDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var categories = _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            return View(categories);
        }
    }
}
