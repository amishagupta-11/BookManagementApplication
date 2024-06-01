using System.ComponentModel.DataAnnotations;

namespace BookManagementApplication.Models
{
    /// <summary>
    /// class represents authors information in the table.
    /// </summary>
    public class AuthorInfo
    {
        [Key]
        public int AuthorId { get; set; }
        [Required]
        public string? Name { get; set; }
        public IEnumerable<AuthorBookInfo>? AuthorBooks { get; set; } 
    }
}
