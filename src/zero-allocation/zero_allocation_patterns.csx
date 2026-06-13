#nullable disable

using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


// Паттерны zero-allocation 


class MyExpensiveObject
{
    public int Data { get; set; }
    public MyExpensiveObject() => Data = 0;
}

class SimpleObjectPool<T> where T : class, new()
{
    private readonly T[] _items;
    private int _index;
    public SimpleObjectPool(int capacity)
    {
        _items = new T[capacity];
        _index = 0;
    }
    public T Get()
    {
        if (_index > 0)
        {
            _index--;
            return _items[_index];
        }
        return new T();
    }
    public void Return(T obj)
    {
        if (_index < _items.Length)
        {
            _items[_index] = obj;
            _index++;
        }
    }
}

struct MyCollection<T>
{
    private readonly T[] _items;
    public MyCollection(T[] items) => _items = items;
    public Enumerator GetEnumerator() => new Enumerator(_items);
    public ref struct Enumerator
    {
        private readonly T[] _items;
        private int _index;
        public Enumerator(T[] items) { _items = items; _index = -1; }
        public T Current => _items[_index];
        public bool MoveNext() { _index++; return _index < _items.Length; }
    }
}

ref struct SpanEnumerator
{
    private readonly ReadOnlySpan<int> _span;
    private int _index;
    public SpanEnumerator(ReadOnlySpan<int> span) { _span = span; _index = -1; }
    public int Current => _span[_index];
    public bool MoveNext() { _index++; return _index < _span.Length; }
    public SpanEnumerator GetEnumerator() => this;
}

// --- Основная логика ---

async Task RunZeroAllocationExamples()
{
    

    // 1. Object Pooling
    Console.WriteLine("1. Object Pooling (переиспользование объектов)");
    var pool = new SimpleObjectPool<MyExpensiveObject>(3);
    var obj1 = pool.Get();
    obj1.Data = 42;
    Console.WriteLine($"   Объект из пула: Data={obj1.Data}");
    pool.Return(obj1);
    var obj2 = pool.Get();
    Console.WriteLine($"   Объект из пула после возврата: Data={obj2.Data} (сброшен в конструкторе)");
    Console.WriteLine();

    // 2. ValueTask<T>
    Console.WriteLine("2. ValueTask<T> (избежание аллокации Task)");
    async ValueTask<int> GetCachedValueAsync(bool cacheHit)
    {
        if (cacheHit) return 42;
        else return await SlowFetchAsync();
    }
    async Task<int> SlowFetchAsync() { await Task.Delay(1); return 42; }
    int cachedResult = await GetCachedValueAsync(true);
    Console.WriteLine($"   Значение из кэша (без аллокации Task): {cachedResult}");
    Console.WriteLine();

    // 3. Итератор без аллокаций (ref struct Enumerator)
    Console.WriteLine("3. Итератор без аллокаций (ref struct Enumerator)");
    ReadOnlySpan<int> numbers = stackalloc int[] { 100, 200, 300 };
    foreach (int n in new SpanEnumerator(numbers))
        Console.WriteLine($"   {n}");
    Console.WriteLine();

    // 4. String.Create – создание строки без промежуточных буферов
    Console.WriteLine("4. String.Create – формирование строки напрямую");
    static string FormatPoint(int x, int y)
    {
        return string.Create(20, (x, y), (span, args) =>
        {
            var (px, py) = args;
            string formatted = $"({px},{py})";
            formatted.AsSpan().CopyTo(span);
        });
    }
    Console.WriteLine($"   Результат: {FormatPoint(5, 10)}");
    Console.WriteLine();

    // 5. Utf8JsonReader – низкоаллокационный парсинг JSON
    Console.WriteLine("5. Utf8JsonReader – без лишних строк");
    byte[] jsonUtf8 = Encoding.UTF8.GetBytes(@"{""name"":""John"",""age"":30}");
    var reader = new Utf8JsonReader(jsonUtf8);
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.PropertyName:
                var propName = reader.ValueSpan;
                Console.WriteLine($"   Свойство: {Encoding.UTF8.GetString(propName)}");
                break;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int age))
                    Console.WriteLine($"   Значение: {age}");
                break;
            case JsonTokenType.String:
                var strVal = reader.ValueSpan;
                Console.WriteLine($"   Строка: {Encoding.UTF8.GetString(strVal)}");
                break;
        }
    }
    Console.WriteLine();

    // 6. Пользовательская коллекция с ref struct Enumerator (foreach без аллокаций)
    Console.WriteLine("6. Коллекция с ref struct Enumerator (без аллокаций)");
    var coll = new MyCollection<int>(new int[] { 1, 2, 3 });
    foreach (int item in coll)
        Console.WriteLine($"   {item}");
    Console.WriteLine();

    // 7. Буферизация с ArrayPool<T>
    Console.WriteLine("7. ArrayPool<T> – временные буферы без аллокаций");
    int[] buffer = ArrayPool<int>.Shared.Rent(256);
    try
    {
        for (int i = 0; i < 256; i++) buffer[i] = i;
        Console.WriteLine($"   Обработано 256 элементов (массив из пула)");
    }
    finally
    {
        ArrayPool<int>.Shared.Return(buffer, clearArray: true);
    }
    Console.WriteLine();

}

await RunZeroAllocationExamples();