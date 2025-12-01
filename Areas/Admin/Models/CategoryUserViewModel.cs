using BookManagementApp.Models;

namespace BookManagementApp.Areas.Admin.Models
{
    public class CategoryUserViewModel
    {
        public IEnumerable<Category>? Categories { get; set; }
        public User? User; 
    }
}
