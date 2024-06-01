using BookManagementApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace BookManagementApplication.Data
{
    /// <summary>
    /// Represents the database context for the Book Management application.
    /// </summary>
    public class BookManagementContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BookManagementContext"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public BookManagementContext(DbContextOptions<BookManagementContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DbSet of authors in the database.
        /// </summary>
        public DbSet<AuthorInfo> Authors { get; set; }

        /// <summary>
        /// Gets or sets the DbSet of books in the database.
        /// </summary>
        public DbSet<Books> Books { get; set; }

        /// <summary>
        /// Gets or sets the DbSet of author-book relationships in the database.
        /// </summary>
        public DbSet<AuthorBookInfo> AuthorBooks { get; set; }

        /// <summary>
        /// Configures the relationships between entities in the database.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance used to configure the database model.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the many-to-many relationship between authors and books
            modelBuilder.Entity<AuthorBookInfo>()
                .HasKey(ab => new { ab.AuthorId, ab.BookId });

            modelBuilder.Entity<AuthorBookInfo>()
                .HasOne(ab => ab.Author)
                .WithMany(a => a.AuthorBooks)
                .HasForeignKey(ab => ab.AuthorId);

            modelBuilder.Entity<AuthorBookInfo>()
                .HasOne(ab => ab.Book)
                .WithMany(b => b.AuthorBooks)
                .HasForeignKey(ab => ab.BookId);
        }
    }
}
