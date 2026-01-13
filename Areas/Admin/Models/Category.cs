using BookManagementApp.Areas.Admin.Models;
using System.ComponentModel.DataAnnotations;

namespace BookManagementApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Display(Name = "Kategori Adı")]
        public string? CategoryName { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public ICollection<Book>? Books { get; set; }
    }
}
//kategori içerisinde kitap listesi olur .Bir kategoride 1den fazla kitap olabilir. Bir kitap birden fazla kategoride olamaz. n=kitap 1=kategori
