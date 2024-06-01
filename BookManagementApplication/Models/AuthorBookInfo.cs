namespace BookManagementApplication.Models
{
    /// <summary>
    /// Represents the relationship between an author and a book in the Book Management application.
    /// </summary>
    public class AuthorBookInfo
    {
        public int AuthorId { get; set; }
        public AuthorInfo? Author { get; set; }

        public int BookId { get; set; }
        public Books? Book { get; set; }
    }
}
