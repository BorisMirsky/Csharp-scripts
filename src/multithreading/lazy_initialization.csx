#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;



// - Lazy<T> гарантирует, что фабрика (valueFactory) вызывается только один раз, даже в многопоточной среде.
// - По умолчанию Lazy<T> использует ExecutionAndPublication (потокобезопасный режим).
// - Можно указать LazyThreadSafetyMode.PublicationOnly – несколько потоков могут вызвать фабрику,
//   но первое завершившееся значение будет опубликовано.
// - LazyThreadSafetyMode.None – не потокобезопасен (быстрее, но только для однопоточного использования).
// - Lazy<T> полезен для ресурсоёмких объектов, которые могут не понадобиться (например, логирование, кэш, подключения).
// - Value – свойство для получения значения (инициализация происходит при первом обращении).
// - IsValueCreated – проверка, было ли значение уже создано.


// 1. Базовое использование (однопоточный сценарий)
Console.WriteLine("1. Базовое использование (однопоточный)");

Lazy<ExpensiveObject> lazyObj = new Lazy<ExpensiveObject>(() => new ExpensiveObject());
Console.WriteLine($"   IsValueCreated: {lazyObj.IsValueCreated}"); // false
ExpensiveObject obj = lazyObj.Value; // инициализация
Console.WriteLine($"   После получения Value: IsValueCreated = {lazyObj.IsValueCreated}");
Console.WriteLine($"   Данные объекта: {obj.Data}\n");


// 2. Потокобезопасная инициализация (ExecutionAndPublication – по умолчанию)
Console.WriteLine("2. Потокобезопасный Lazy (ExecutionAndPublication)");

Lazy<int> safeLazy = new Lazy<int>(() =>
{
    Console.WriteLine("   Фабрика выполняется один раз");
    return 42;
});

Parallel.For(0, 5, _ =>
{
    int val = safeLazy.Value;
    Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId}: получил {val}");
});
Console.WriteLine();


// 3. Lazy с режимом PublicationOnly (несколько фабрик, побеждает первая завершившаяся)
Console.WriteLine("3. Режим PublicationOnly (несколько попыток, публикуется первое завершённое значение)");

Lazy<int> pubLazy = new Lazy<int>(() =>
{
    int tid = Thread.CurrentThread.ManagedThreadId;
    Console.WriteLine($"   Фабрика на потоке {tid} начала работу");
    Thread.Sleep(50); // имитация разной длительности
    Console.WriteLine($"   Фабрика на потоке {tid} завершилась");
    return tid;
}, LazyThreadSafetyMode.PublicationOnly);

Parallel.For(0, 4, _ =>
{
    int result = pubLazy.Value;
    Console.WriteLine($"   Значение от {result} получено");
});
Console.WriteLine();


// 4. Lazy без потокобезопасности (LazyThreadSafetyMode.None) – фабрика вызывается несколько раз
Console.WriteLine("4. Режим None (не потокобезопасен) – фабрика вызывается многократно");

int factoryCallCount = 0;
object factoryLock = new object();
Lazy<int> unsafeLazy = new Lazy<int>(() =>
{
    lock (factoryLock)
    {
        int count = ++factoryCallCount;
        Console.WriteLine($"   Фабрика вызвана #{count}");
    }
    Thread.Sleep(50); // имитация работы
    return 123;
}, LazyThreadSafetyMode.None);

Thread[] threads2 = new Thread[3];
for (int i = 0; i < threads2.Length; i++)
{
    threads2[i] = new Thread(() =>
    {
        try
        {
            int value = unsafeLazy.Value;
            Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} получил {value}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} перехватил: {ex.Message}");
        }
    });
}
foreach (var t in threads2) t.Start();
foreach (var t in threads2) t.Join();
Console.WriteLine($"   Фабрика была вызвана {factoryCallCount} раз (при гонке >1)\n");


// 5. Lazy с исключением (фабрика выбрасывает ошибку)
Console.WriteLine("5. Исключение в фабрике");

Lazy<int> brokenLazy = new Lazy<int>(() =>
{
    throw new InvalidOperationException("Ошибка инициализации");
});

try
{
    int val = brokenLazy.Value;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   Исключение: {ex.Message}");
}
Console.WriteLine($"   IsValueCreated = {brokenLazy.IsValueCreated} (остаётся false)\n");


// 6. Практический пример: ленивый кэш с очисткой
Console.WriteLine("6. Практический пример: ленивый кэш");

class DataCache
{
    private Lazy<Dictionary<string, string>> _lazyData = new Lazy<Dictionary<string, string>>(() =>
    {
        Console.WriteLine("   Загрузка данных в кэш...");
        Thread.Sleep(100);
        return new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
    });
    
    public Dictionary<string, string> Data => _lazyData.Value;
    public void Invalidate() => _lazyData = new Lazy<Dictionary<string, string>>(() =>
    {
        Console.WriteLine("   Перезагрузка кэша...");
        return new Dictionary<string, string>();
    });
}

var cache = new DataCache();
Console.WriteLine($"   Доступа к данным ещё не было, IsValueCreated = {cache.Data.GetType() != null}");
var data = cache.Data;
Console.WriteLine($"   После обращения: key1 = {data["key1"]}");
cache.Invalidate();
var newData = cache.Data;
Console.WriteLine($"   После инвалидации: количество элементов = {newData.Count}\n");



// 7. Альтернатива: LazyInitializer (статический класс для ленивой инициализации полей)
Console.WriteLine("7. LazyInitializer – без создания объекта Lazy<T>");

class MyService
{
    private object _lock = new object();
    private ExpensiveObject _field;
    public ExpensiveObject Instance => LazyInitializer.EnsureInitialized(ref _field, ref _lock, () =>
    {
        Console.WriteLine("   LazyInitializer создаёт экземпляр");
        return new ExpensiveObject();
    });
}

var service = new MyService();
Console.WriteLine($"   Instance до вызова: {service.Instance == null}");
var inst = service.Instance;
Console.WriteLine($"   Instance после вызова: Data={inst.Data}\n");

Console.WriteLine("=== Конец раздела Lazy<T> ===");

// Вспомогательный класс для демонстрации
class ExpensiveObject
{
    public string Data { get; }
    public ExpensiveObject()
    {
        Data = $"Created at {DateTime.Now:mm:ss.fff}";
    }
}