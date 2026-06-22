#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices; 


// - Конструкторы не могут быть async. Используйте фабричные методы, Lazy<T> или отдельный InitializeAsync().
// - using можно использовать с await для асинхронного освобождения ресурсов (IAsyncDisposable).
// - IAsyncDisposable – для ресурсов, требующих асинхронной очистки (например, сетевые соединения, файлы).
// - IAsyncEnumerable<T> – асинхронный поток данных, можно использовать await foreach.

// Что демонстрируется
// Async в конструкторах – фабричный метод, Lazy<Task<T>>, отдельный InitializeAsync().
// IAsyncDisposable и await using – создание ресурса, работа, асинхронное освобождение.
// IAsyncEnumerable<T> – асинхронный генератор с yield return и await внутри, потребление через await foreach.
// Комбинация IAsyncDisposable + IAsyncEnumerable – продвинутый сценарий.
// Отмена в асинхронных потоках – передача CancellationToken.





// 1. Async в конструкторе – обход через фабричный метод
Console.WriteLine("1. Async в конструкторе (фабричный метод):");

class AsyncService
{
    public string Data { get; private set; }

    // Приватный конструктор
    private AsyncService() { }

    // Фабричный метод для асинхронной инициализации
    public static async Task<AsyncService> CreateAsync()
    {
        var service = new AsyncService();
        // Асинхронная инициализация (например, загрузка данных)
        await Task.Delay(100); // имитация
        service.Data = "Инициализировано асинхронно";
        return service;
    }
}

AsyncService service = await AsyncService.CreateAsync();
Console.WriteLine($"   Данные сервиса: {service.Data}");
Console.WriteLine();




// 2. Альтернатива: Lazy<T> + async (инициализация при первом обращении)
Console.WriteLine("2. Lazy<T> с асинхронной инициализацией:");

class LazyAsyncService
{
    private readonly Lazy<Task<string>> _lazyData;

    public LazyAsyncService()
    {
        _lazyData = new Lazy<Task<string>>(async () =>
        {
            await Task.Delay(50);
            return "Данные из Lazy";
        });
    }

    public Task<string> GetDataAsync() => _lazyData.Value;
}

var lazyService = new LazyAsyncService();
string data = await lazyService.GetDataAsync();
Console.WriteLine($"   {data}");
Console.WriteLine();


// 3. IAsyncDisposable и await using
Console.WriteLine("3. IAsyncDisposable (асинхронное освобождение):");

class AsyncResource : IAsyncDisposable
{
    private bool _disposed;

    public async Task UseAsync()
    {
        Console.WriteLine("   Использование ресурса...");
        await Task.Delay(50);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            Console.WriteLine("   Асинхронное освобождение ресурса (например, закрытие соединения)");
            await Task.Delay(50);
        }
    }
}

// Использование с await using
await using (var resource = new AsyncResource())
{
    await resource.UseAsync();
}
// Здесь автоматически вызывается DisposeAsync()
Console.WriteLine();




// 4. Использование IAsyncDisposable с try/finally (для старых версий)
Console.WriteLine("4. IAsyncDisposable с try/finally (если await using недоступен):");

async Task ManualAsyncDisposeExample()
{
    var resource = new AsyncResource();
    try
    {
        await resource.UseAsync();
    }
    finally
    {
        await resource.DisposeAsync();
    }
}

await ManualAsyncDisposeExample();
Console.WriteLine();



// 5. IAsyncEnumerable<T> – асинхронный поток данных
Console.WriteLine("5. IAsyncEnumerable<T> (асинхронный стрим):");

async IAsyncEnumerable<int> ProduceDataAsync(int count, int delayMs)
{
    for (int i = 0; i < count; i++)
    {
        await Task.Delay(delayMs);
        yield return i;
    }
}

Console.WriteLine("   Получение данных через await foreach:");
await foreach (int item in ProduceDataAsync(5, 100))
{
    Console.WriteLine($"      {item}");
}
Console.WriteLine();




// 6. IAsyncEnumerable с CancellationToken
Console.WriteLine("6. IAsyncEnumerable с отменой:");

// Добавляем атрибут [EnumeratorCancellation] для передачи токена от вызывающего кода
async IAsyncEnumerable<int> ProduceDataWithCancellationAsync(
    int count, 
    int delayMs, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (int i = 0; i < count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(delayMs, cancellationToken);
        yield return i;
    }
}

using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(350);
    Console.WriteLine("   Запуск с отменой через 350 мс");
    try
    {
        await foreach (var item in ProduceDataWithCancellationAsync(10, 100, cts.Token))
        {
            Console.WriteLine($"      {item}");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("   Операция отменена");
    }
}
Console.WriteLine();


// 7. Комбинирование IAsyncEnumerable с LINQ (через System.Linq.Async – не встроен, покажем без)
Console.WriteLine("7. IAsyncEnumerable – обработка без LINQ (для примера):");

async Task ProcessAsyncStream(IAsyncEnumerable<int> stream)
{
    int sum = 0;
    await foreach (var item in stream)
    {
        sum += item;
        Console.WriteLine($"   Промежуточная сумма: {sum}");
    }
    Console.WriteLine($"   Итоговая сумма: {sum}");
}

await ProcessAsyncStream(ProduceDataAsync(4, 80));
Console.WriteLine();

