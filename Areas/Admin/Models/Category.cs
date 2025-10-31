namespace BookManagementApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? CategoryName { get; set; }
        public ICollection<Book>? Books { get; set; }
    }
}
//kategori içerisinde kitap listesi olur .Bir kategoride 1den fazla kitap olabilir. Bir kitap birden fazla kategoride olamaz. n=kitap 1=kategori
