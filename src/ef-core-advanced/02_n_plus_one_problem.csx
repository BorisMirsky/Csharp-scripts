
#nullable disable


// 2. ПРОБЛЕМА N+1 ЗАПРОСОВ В EF CORE

// - Проблема N+1 возникает, когда вы загружаете коллекцию сущностей (N штук),
//   а затем для каждой из них отдельно подгружаете связанные данные.
// - Вместо одного JOIN-запроса выполняется N+1 запросов (1 на коллекцию + N на каждую связанную сущность).
// - Это приводит к падению производительности при большом N.
// - Решение: использовать Eager Loading (Include / ThenInclude) или Projection (Select).
// - В примере ниже покажем разницу на практике с подсчётом выполненных SQL-запросов.


#r "nuget: Microsoft.EntityFrameworkCore, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 8.0.0"

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;


// СЧЁТЧИК ЗАПРОСОВ (через LogTo)
public class QueryCounter
{
    private int _count;
    public int Count => _count;
    public void Reset() => _count = 0;

    public void Log(string message)
    {
        // Игнорируем системные запросы к sqlite_master и CREATE/INDEX
        if (!message.Contains("sqlite_master") && message.Contains("Executed") && !message.Contains("CREATE"))
            _count++;
    }
}


// ОПРЕДЕЛЕНИЕ МОДЕЛИ
public class Blog
{
    public int BlogId { get; set; }
    public string Name { get; set; }
    public List<Post> Posts { get; set; } = new();
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; }
}


// КОНТЕКСТ БАЗЫ ДАННЫХ
public class AppDbContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    private readonly SqliteConnection _connection;
    private readonly QueryCounter _counter;

    public AppDbContext(SqliteConnection connection, QueryCounter counter)
    {
        _connection = connection;
        _counter = counter;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Используем переданное соединение (уже открытое)
        optionsBuilder.UseSqlite(_connection);
        // Логируем SQL в консоль и передаём в счётчик
        optionsBuilder.LogTo(
            message => {
                Console.WriteLine($"   SQL: {message}");
                _counter.Log(message);
            },
            LogLevel.Information
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>()
            .HasMany(b => b.Posts)
            .WithOne(p => p.Blog)
            .HasForeignKey(p => p.BlogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


// ТОЧКА ВХОДА

try
{
    var counter = new QueryCounter();

    // Создаём соединение и открываем его
    using var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();

    using (var db = new AppDbContext(connection, counter))
    {
        db.Database.EnsureCreated();

        // Заполняем тестовыми данными, если их нет
        if (!db.Blogs.Any())
        {
            Console.WriteLine("Заполнение тестовыми данными...");
            var blogs = new[]
            {
                new Blog
                {
                    Name = "Блог о C#",
                    Posts = new List<Post>
                    {
                        new Post { Title = "Введение в LINQ", Content = "LINQ – это..." },
                        new Post { Title = "Async/await", Content = "Асинхронность..." }
                    }
                },
                new Blog
                {
                    Name = "Блог о Python",
                    Posts = new List<Post>
                    {
                        new Post { Title = "Декораторы", Content = "Декораторы в Python..." },
                        new Post { Title = "Генераторы", Content = "Генераторы – это..." },
                        new Post { Title = "Асинхронность в Python", Content = "async/await в Python" }
                    }
                },
                new Blog
                {
                    Name = "Блог о Go",
                    Posts = new List<Post>
                    {
                        new Post { Title = "Горутины", Content = "Горутины в Go..." }
                    }
                }
            };
            db.Blogs.AddRange(blogs);
            db.SaveChanges();
        }


        // 1. ПРОБЛЕМА N+1 (БЕЗ INCLUDE)
        Console.WriteLine("\n1. Без Include (проблема N+1):");
        counter.Reset();

        var blogsWithoutInclude = db.Blogs.ToList();

        foreach (var blog in blogsWithoutInclude)
        {
            Console.WriteLine($"   Блог: {blog.Name}");
            foreach (var post in blog.Posts)
            {
                Console.WriteLine($"      - {post.Title}");
            }
        }

        Console.WriteLine($"   Выполнено запросов: {counter.Count} (должно быть 1 + N = {1 + blogsWithoutInclude.Count})");


        // 2. РЕШЕНИЕ: ИСПОЛЬЗОВАНИЕ INCLUDE (EAGER LOADING)
        Console.WriteLine("\n2. С Include (решение проблемы):");
        counter.Reset();

        var blogsWithInclude = db.Blogs
            .Include(b => b.Posts)
            .ToList();

        foreach (var blog in blogsWithInclude)
        {
            Console.WriteLine($"   Блог: {blog.Name}");
            foreach (var post in blog.Posts)
            {
                Console.WriteLine($"      - {post.Title}");
            }
        }

        Console.WriteLine($"   Выполнено запросов: {counter.Count} (должен быть 1)");


        // 3. АЛЬТЕРНАТИВА: ПРОЕКЦИЯ ЧЕРЕЗ SELECT
        Console.WriteLine("\n3. Альтернатива: проекция через Select (только нужные поля):");
        counter.Reset();

        var projection = db.Blogs
            .Select(b => new
            {
                b.Name,
                PostTitles = b.Posts.Select(p => p.Title)
            })
            .ToList();

        foreach (var item in projection)
        {
            Console.WriteLine($"   Блог: {item.Name}");
            foreach (var title in item.PostTitles)
            {
                Console.WriteLine($"      - {title}");
            }
        }

        Console.WriteLine($"   Выполнено запросов: {counter.Count} (1 запрос, но только нужные поля)");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}