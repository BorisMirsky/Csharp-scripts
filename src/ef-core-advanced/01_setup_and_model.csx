#nullable disable


// Что демонстрируется
// Подключение пакетов EF Core и In-Memory через #r "nuget:...".
// Определение сущностей Blog и Post с навигационными свойствами.
// Класс AppDbContext с настройкой In-Memory Database и Fluent API.
// Создание базы через EnsureCreated().
// Добавление тестовых данных (блоги с постами).
// Загрузка данных с Include и вывод в консоль.
// Информация о модели.



// НАСТРОЙКА EF CORE: МОДЕЛЬ, DbContext, In-Memory БАЗА

// Подключаем необходимые NuGet-пакеты
#r "nuget: Microsoft.EntityFrameworkCore, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.InMemory, 8.0.0"

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;


// ОПРЕДЕЛЕНИЕ МОДЕЛИ (сущности)
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


// КОНТЕКСТ БАЗЫ ДАННЫХ (DbContext)
public class AppDbContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("DemoDb");
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


// ТОЧКА ВХОДА (TOP-LEVEL STATEMENTS)

try
{
    Console.WriteLine("1. Модель и DbContext определены");

    using (var db = new AppDbContext())
    {
        Console.WriteLine("2. Создание базы данных...");
        bool created = db.Database.EnsureCreated();
        Console.WriteLine($"   База создана: {created} (при первом запуске будет true)");
        Console.WriteLine($"   In-Memory: {db.Database.IsInMemory()}");

        Console.WriteLine("\n3. Заполнение тестовыми данными...");

        if (!db.Blogs.Any())
        {
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
            int saved = db.SaveChanges();
            Console.WriteLine($"   Добавлено {saved} записей");
        }
        else
        {
            Console.WriteLine("   Данные уже существуют, пропускаем добавление");
        }

        Console.WriteLine("\n4. Текущее состояние базы:");

        var blogList = db.Blogs
            .Include(b => b.Posts)
            .ToList();

        if (blogList.Count == 0)
        {
            Console.WriteLine("   Блогов нет (что-то пошло не так)");
        }
        else
        {
            foreach (var blog in blogList)
            {
                Console.WriteLine($"   Блог: {blog.Name} (ID={blog.BlogId})");
                foreach (var post in blog.Posts)
                {
                    Console.WriteLine($"      - {post.Title}");
                }
            }
        }

        Console.WriteLine("\n5. Модель базы данных:");
        var entityTypes = db.Model.GetEntityTypes().Select(e => e.Name);
        Console.WriteLine($"   Сущности: {string.Join(", ", entityTypes)}");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}