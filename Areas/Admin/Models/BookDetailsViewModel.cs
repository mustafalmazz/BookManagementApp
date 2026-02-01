using BookManagementApp.Models;

namespace BookManagementApp.Areas.Admin.Models
{
    public class BookDetailsViewModel
    {
        public Book? Book { get; set; }
        public IEnumerable<Book>? RelatedBooks { get; set; }

        // YENİ EKLENEN - Kategoriler listesi
        public IEnumerable<Category>? AllCategories { get; set; }
    }
}
