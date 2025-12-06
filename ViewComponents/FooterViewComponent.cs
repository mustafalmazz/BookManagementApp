using Microsoft.AspNetCore.Mvc;

namespace BookManagementApp.ViewComponents
{
    public class FooterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
           
            return View();
        }
    }
}
