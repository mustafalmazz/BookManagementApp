using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public IActionResult Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("List");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var list = _context.Categories
                .Where(c => c.UserId == userId && (string.IsNullOrEmpty(q) || c.CategoryName.Contains(q))).Include(x => x.Books)
                .ToList();

            return View("List",list);
        }
        public IActionResult List()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var list = _context.Categories
                .Where(x => x.UserId == userId)
                .Include(x => x.Books)
                .ToList();

            return View(list);
        }

     
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View();
        }

       
        [HttpPost]
        public IActionResult Create(Category category)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            category.UserId = userId.Value; 
            _context.Categories.Add(category);
            _context.SaveChanges();

            return RedirectToAction("List");
        }

        public IActionResult Edit(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var category = _context.Categories
                .Include(x => x.Books)
                .FirstOrDefault(x => x.Id == id && x.UserId == userId); 

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryEditViewModel
            {
                Books = _context.Books.Where(b => b.UserId == userId).ToList(),
                Category = category
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (category == null)
            {
                return NotFound();
            }

            var find = _context.Categories.FirstOrDefault(c => c.Id == category.Id && c.UserId == userId);
            if (find == null)
            {
                return NotFound();
            }

            find.CategoryName = category.CategoryName;
            _context.SaveChanges();

            return RedirectToAction("List");
        }

        public IActionResult Delete(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var bul = _context.Categories.FirstOrDefault(c => c.Id == id && c.UserId == userId);
            if (bul == null)
            {
                return NotFound();
            }

           

            return View(bul);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            var find = _context.Categories.FirstOrDefault(c=>c.UserId == userId && c.Id == id);
            _context.Categories.Remove(find);
            _context.SaveChanges();
            return RedirectToAction("List");
        }
    }
}
