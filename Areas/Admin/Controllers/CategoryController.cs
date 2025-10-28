using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace BookManagementApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly MyDbContext _context;
        public CategoryController(MyDbContext context)
        {
                _context = context;
        }
        public IActionResult List()
        {
            var list = _context.Categories.Include(x=>x.Books).ToList();
            return View(list);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]  
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            if (category == null)
            {
                return NotFound();
            }
            _context.Categories.Add(category);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = _context.Categories.Include(x => x.Books).FirstOrDefault(x=>x.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            var viewModel = new CategoryEditViewModel
            {
                Books = _context.Books.ToList(),
                Category = category
            };
            

            return View(viewModel);
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if(category == null)
            {
                return NotFound();
            }
            var find = _context.Categories.FirstOrDefault(c => c.Id == category.Id);
            if (find == null)
            {
                return NotFound();
            }
            find.Id = category.Id;
            find.CategoryName = category.CategoryName;
            _context.SaveChanges();
            return RedirectToAction("List");
        }
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var bul = _context.Categories.Find(id);
            if (bul == null)
            {
                return NotFound();
            }
            _context.Categories.Remove(bul);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
