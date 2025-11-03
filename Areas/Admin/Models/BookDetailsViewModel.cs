using BookManagementApp.Models;

namespace BookManagementApp.Areas.Admin.Models
{
    public class BookDetailsViewModel
    {
        public List<Book>?  RelatedBooks{ get; set; }
        public Book? Book { get; set; }
    }
}
