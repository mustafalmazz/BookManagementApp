using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookManagementApp.ViewComponents
{
    public class NavbarViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Eğer kategori ve kullanıcı bilgisini servisten çekiyorsanız
        // ilgili servisleri buraya inject edin.
        // Örnek: private readonly ICategoryService _categoryService;
        //        private readonly IUserService _userService;

        public NavbarViewComponent(IHttpContextAccessor httpContextAccessor
            /* , ICategoryService categoryService, IUserService userService */)
        {
            _httpContextAccessor = httpContextAccessor;
            // _categoryService = categoryService;
            // _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ---- Kategorileri ve kullanıcıyı burada çekin ----
            // Aşağıdaki satırları kendi servis/repository yapınıza göre doldurun:

            // var categories = await _categoryService.GetAllAsync();
            // var user       = await _userService.GetCurrentUserAsync(HttpContext.User);

            // Şimdilik boş model dönüyoruz — servislerinizi ekleyince doldurun:
            var model = new CategoryUserViewModel
            {
                Categories = new List<Category>(),   // await _categoryService.GetAllAsync()
                User = null                     // await _userService.GetCurrentAsync()
            };

            return View(model);
        }
    }
}