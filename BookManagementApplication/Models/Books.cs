using System.ComponentModel.DataAnnotations;

namespace BookManagementApplication.Models
{
    /// <summary>
    /// class represents books information in the table.
    /// </summary>
    public class Books
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        public string? Title { get; set; }

        [Required]
        public DateTime PublicationDate { get; set; }

        [Required]
        [StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ISBN must be a 13-digit number.")]
        public string? ISBN { get; set; }

        public IEnumerable<AuthorBookInfo>? AuthorBooks { get; set; } 
    }
}
