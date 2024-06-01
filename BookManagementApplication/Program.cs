using BookManagementApplication.Data;
using BookManagementApplication.Models;
using Microsoft.EntityFrameworkCore;


namespace BookManagementApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<BookManagementContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("BookManagement")));
            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // Endpoint definitions

            app.MapGet("/books", (BookManagementContext db) => GetBooks(db));

            app.MapGet("/books/{id}", (int id, BookManagementContext db) => GetBookById(id, db));

            app.MapPost("/books", (AuthorBookInfo authorBookInfo, BookManagementContext db) => CreateBook(authorBookInfo, db));

            app.MapPut("/books/{id}", (int id, Books updatedBook, string authorName, BookManagementContext db) => UpdateBook(id, updatedBook, authorName, db));

            app.MapDelete("/books/{id}", (int id, BookManagementContext db) => DeleteBook(id, db));

            app.MapGet("/authors", (BookManagementContext db) => GetAuthors(db));

            app.MapGet("/authors/{id}", (int id, BookManagementContext db) => GetAuthorById(id, db));

            app.MapPost("/authors", (AuthorInfo author, BookManagementContext db) => CreateAuthor(author, db));

            app.MapPut("/authors/{id}", (int id, AuthorBookInfo authorBookInfo, BookManagementContext db) => UpdateAuthor(id, authorBookInfo, db));

            app.MapDelete("/authors/{id}", (int id, BookManagementContext db) => DeleteAuthor(id, db));

            app.Run();
        }

        /// <summary>
        /// method to fetch books from the database.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult GetBooks(BookManagementContext db)
        {
            try
            {
                var books = db.Books.Include(b => b.AuthorBooks).ThenInclude(ba => ba.Author).ToList();
                return Results.Ok(books);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error fetching books: {ex.Message}");
            }
        }

        /// <summary>
        /// method to fetch data by id of books.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static IResult GetBookById(int id, BookManagementContext db)
        {
            try
            {
                var book = db.Books.Include(b => b.AuthorBooks).ThenInclude(ba => ba.Author).FirstOrDefault(b => b.BookId == id);
                return book is not null ? Results.Ok(book) : Results.NotFound("Book not found.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error fetching book by ID: {ex.Message}");
            }
        }

        /// <summary>
        /// method to insert books data in database.
        /// </summary>
        /// <param name="authorBookInfo"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static IResult CreateBook(AuthorBookInfo authorBookInfo, BookManagementContext db)
        {
            try
            {
                if (authorBookInfo == null || authorBookInfo.Book == null || string.IsNullOrWhiteSpace(authorBookInfo.Book.Title) || string.IsNullOrWhiteSpace(authorBookInfo.Book.ISBN) || authorBookInfo.Book.PublicationDate == default)
                {
                    return Results.BadRequest("Invalid book data.");
                }

                if (db.Books.Any(b => b.ISBN == authorBookInfo.Book.ISBN))
                {
                    return Results.BadRequest("ISBN must be unique.");
                }

                var author = db.Authors.FirstOrDefault(a => a.AuthorId == authorBookInfo.AuthorId);
                if (author == null)
                {
                    return Results.NotFound("Author not found.");
                }

                // Clear the AuthorBooks navigation property to prevent circular reference during serialization
                authorBookInfo.Book.AuthorBooks = null;

                // Add the book to the database
                db.Books.Add(authorBookInfo.Book);
                db.SaveChanges();

                // Assign the book ID
                authorBookInfo.BookId = authorBookInfo.Book.BookId;

                // Add the author-book relationship to the database
                db.AuthorBooks.Add(authorBookInfo);
                db.SaveChanges();

                // Now, retrieve the book with its author and return the result
                var createdBook = db.Books
                    .Include(b => b.AuthorBooks)
                    .ThenInclude(ab => ab.Author)
                    .FirstOrDefault(b => b.BookId == authorBookInfo.BookId);

                return Results.Created($"/books/{authorBookInfo.BookId}", createdBook);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error creating book: {ex.Message}");
            }
        }

        /// <summary>
        /// method to update books details in the database.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updatedBook"></param>
        /// <param name="authorName"></param>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult UpdateBook(int id, Books updatedBook, string authorName, BookManagementContext db)
        {
            try
            {
                var book = db.Books.Include(b => b.AuthorBooks).ThenInclude(ba => ba.Author).FirstOrDefault(b => b.BookId == id);
                if (book is null) return Results.NotFound("Book not found.");

                if (string.IsNullOrWhiteSpace(updatedBook.Title) || string.IsNullOrWhiteSpace(updatedBook.ISBN) || updatedBook.PublicationDate == default)
                {
                    return Results.BadRequest("Invalid book data.");
                }

                if (db.Books.Any(b => b.ISBN == updatedBook.ISBN && b.BookId != id))
                {
                    return Results.BadRequest("ISBN must be unique.");
                }

                book.Title = updatedBook.Title;
                book.PublicationDate = updatedBook.PublicationDate;
                book.ISBN = updatedBook.ISBN;

                db.AuthorBooks.RemoveRange(book.AuthorBooks);

                var author = db.Authors.FirstOrDefault(a => a.Name == authorName) ?? new AuthorInfo { Name = authorName };

                if (author.AuthorId == 0)
                {
                    db.Authors.Add(author);
                    db.SaveChanges();
                }

                db.AuthorBooks.Add(new AuthorBookInfo { BookId = book.BookId, AuthorId = author.AuthorId });
                db.SaveChanges();

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error updating book: {ex.Message}");
            }
        }

        /// <summary>
        /// method to delete books data from the database.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult DeleteBook(int id, BookManagementContext db)
        {
            try
            {
                var book = db.Books.Include(b => b.AuthorBooks).FirstOrDefault(b => b.BookId == id);
                if (book is null) return Results.NotFound("Book not found.");

                db.Books.Remove(book);
                db.SaveChanges();
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error deleting book: {ex.Message}");
            }
        }

        /// <summary>
        /// method to get authors data from the database.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        private static IResult GetAuthors(BookManagementContext db)
        {
            try
            {
                var authors = db.Authors.Include(a => a.AuthorBooks).ThenInclude(ba => ba.Book).ToList();
                var result = authors.Select(a => new
                {
                    a.AuthorId,
                    a.Name,
                    Books = a.AuthorBooks.Select(ba => ba.Book.Title)
                });
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error fetching authors: {ex.Message}");
            }
        }

        /// <summary>
        /// method to fetch authors data by id from the database.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static IResult GetAuthorById(int id, BookManagementContext db)
        {
            try
            {
                var author = db.Authors.Include(a => a.AuthorBooks).ThenInclude(ba => ba.Book).FirstOrDefault(a => a.AuthorId == id);
                if (author == null) return Results.NotFound("Author not found.");

                var result = new
                {
                    author.AuthorId,
                    author.Name,
                    Books = author.AuthorBooks.Select(ba => ba.Book.Title)
                };
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error fetching author by ID: {ex.Message}");
            }
        }


        /// <summary>
        /// method to insert the authors data into the database.
        /// </summary>
        /// <param name="authorInfo"></param>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult CreateAuthor(AuthorInfo authorInfo, BookManagementContext db)
        {
            try
            {
                if (authorInfo == null || string.IsNullOrWhiteSpace(authorInfo.Name))
                {
                    return Results.BadRequest("Invalid author data.");
                }

                db.Authors.Add(authorInfo);
                db.SaveChanges();

                return Results.Created($"/authors/{authorInfo.AuthorId}", authorInfo);
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error creating author: {ex.Message}");
            }
        }

        /// <summary>
        /// method to update author information in the database by passing id 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="authorBookInfo"></param>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult UpdateAuthor(int id, AuthorBookInfo authorBookInfo, BookManagementContext db)
        {
            try
            {
                var author = db.Authors.FirstOrDefault(a => a.AuthorId == id);
                if (author == null) return Results.NotFound("Author not found.");

                if (authorBookInfo.Author == null || string.IsNullOrWhiteSpace(authorBookInfo.Author.Name))
                {
                    return Results.BadRequest("Invalid author data.");
                }

                author.Name = authorBookInfo.Author.Name;
                db.SaveChanges();

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error updating author: {ex.Message}");
            }
        }

        /// <summary>
        /// method to delete the authod information from the database.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="db"></param>
        /// <returns></returns>

        private static IResult DeleteAuthor(int id, BookManagementContext db)
        {
            try
            {
                var author = db.Authors.Include(a => a.AuthorBooks).FirstOrDefault(a => a.AuthorId == id);
                if (author == null) return Results.NotFound("Author not found.");

                db.Authors.Remove(author);
                db.SaveChanges();
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error deleting author: {ex.Message}");
            }
        }
    }
}

