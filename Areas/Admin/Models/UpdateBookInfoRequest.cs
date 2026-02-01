namespace BookManagementApp.Areas.Admin.Models
{
    public class UpdateBookInfoRequest
    {
        public int BookId { get; set; }
        public string? Name { get; set; }
        public string? Author { get; set; }
        public int CategoryId { get; set; }
        public int TotalPages { get; set; }
        public decimal? Rating { get; set; }
    }
}
