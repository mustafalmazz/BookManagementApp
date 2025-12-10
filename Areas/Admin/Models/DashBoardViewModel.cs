using BookManagementApp.Models;

namespace BookManagementApp.Areas.Admin.Models
{
    public class DashBoardViewModel
    {
        public List<Book>? Books { get; set; }
        public List<Category>? Categories { get; set; }
        public List<User>? Users { get; set; }
    }
}
