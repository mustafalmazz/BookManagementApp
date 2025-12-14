using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BookManagementApp.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    public class CategoriesController : Controller
    {
        private readonly MyDbContext _context;
        public CategoriesController(MyDbContext context)
        {
            _context = context;
        }
        public IActionResult List()
        {
            var list = _context.Categories.Include(c=>c.User).OrderByDescending(c => c.Id).ToList();
            return View(list);
        }
        public IActionResult DeleteConfirm(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
        [HttpPost]
        public IActionResult Delete(Category category)
        {
            var existingCategory = _context.Categories.FirstOrDefault(c=>c.Id == category.Id);
            if (existingCategory == null)
            {
                return NotFound();
            }
            _context.Categories.Remove(existingCategory);
            _context.SaveChanges();

            return RedirectToAction("List");

        }
        public IActionResult Edit(int id)
        {
            var cat = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null)
            {
                return NotFound();
            }

            return View(cat);
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            var existingCategory = _context.Categories.FirstOrDefault(c => c.Id == category.Id);
            if (existingCategory == null)
            {
                return NotFound();
            }
            existingCategory.CategoryName = category.CategoryName;
            _context.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
