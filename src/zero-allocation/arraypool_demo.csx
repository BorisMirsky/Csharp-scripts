#nullable disable

using System;
using System.Buffers;
using System.Threading.Tasks;

// Для уменьшения аллокаций в современном .NET есть разные способы.
// Иногда объект можно переиспользовать. Для самых крупных объектов — массивов
// в .NET встроены несколько реализаций ArrayPool<T>.


// Структура-владелец с реализацией IDisposable для using
public struct ArrayPoolOwner<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    private T[] _array;

    public ArrayPoolOwner(ArrayPool<T> pool, int minimumLength)
    {
        _pool = pool;
        _array = pool.Rent(minimumLength);
    }

    public Span<T> Span => _array.AsSpan(0, _array.Length);

    public void Dispose()
    {
        if (_array != null)
        {
            _pool.Return(_array, clearArray: true);
            _array = null;
        }
    }
}

async Task RunExamples()
{
	
    // 1. Базовое использование
    Console.WriteLine("1. Базовое использование:");
    int[] rentedArray = ArrayPool<int>.Shared.Rent(10);  // Rent извлекает буфер запрошенной длины.
    try
    {
        Console.WriteLine($"Арендован массив длиной {rentedArray.Length} (запрошено 10)");
        for (int i = 0; i < 10; i++) rentedArray[i] = i * i;
        Console.WriteLine($"Первые 5 элементов: {rentedArray[0]}, {rentedArray[1]}, {rentedArray[2]}, {rentedArray[3]}, {rentedArray[4]}");
    }
    finally
    {
        ArrayPool<int>.Shared.Return(rentedArray, clearArray: true);
        Console.WriteLine("Массив возвращён в пул\n");
    }

    // 2. Сравнение аллокаций
    Console.WriteLine("2. Сравнение аллокаций:");
    const int iterations = 1000;
    for (int i = 0; i < iterations; i++)
    {
        var temp = new byte[256];
    }
    Console.WriteLine($"Создание {iterations} массивов new byte[256] генерирует {iterations} аллокаций.");

    for (int i = 0; i < iterations; i++)
    {
        byte[] temp = ArrayPool<byte>.Shared.Rent(256);
        try { /* работа */ }
        finally { ArrayPool<byte>.Shared.Return(temp); }
    }
    Console.WriteLine($"Аренда из пула: аллокаций значительно меньше.\n");

    // 3. Без очистки (утечка данных)
    Console.WriteLine("3. Без очистки массива (утечка данных):");
    char[] buffer;
    buffer = ArrayPool<char>.Shared.Rent(5);
    try
    {
        "Hi!".AsSpan().CopyTo(buffer.AsSpan());
        Console.WriteLine($"Первый арендатор записал: '{new string(buffer, 0, 3)}'");
    }
    finally { ArrayPool<char>.Shared.Return(buffer, clearArray: false); }

    buffer = ArrayPool<char>.Shared.Rent(5);
    try
    {
        Console.WriteLine($"Второй арендатор видит старые данные: '{new string(buffer)}'");
    }
    finally { ArrayPool<char>.Shared.Return(buffer, clearArray: true); }
    Console.WriteLine();

    // 4. Асинхронная обработка
    async Task ProcessDataAsync()
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            Memory<byte> memory = rented.AsMemory(0, 4096);
            memory.Span.Fill(0xAA);
            await Task.Delay(10);
            Console.WriteLine($"Обработано {memory.Length} байт");
        }
        finally { ArrayPool<byte>.Shared.Return(rented); }
    }
    await ProcessDataAsync();
    Console.WriteLine();

    // 5. Обёртка с using (теперь работает)
    Console.WriteLine("5. Обёртка с using (ArrayPoolOwner):");
    using (var owner = new ArrayPoolOwner<int>(ArrayPool<int>.Shared, 100))
    {
        Span<int> span = owner.Span;
        for (int i = 0; i < 100; i++) span[i] = i;
        Console.WriteLine($"Последний элемент: {span[99]}");
    }
    Console.WriteLine();

    // 6. Ошибка: возврат арендованного массива
    Console.WriteLine("6. ОШИБОЧНЫЙ паттерн: возврат арендованного массива из метода");
    Console.WriteLine("   public int[] GetData() { var arr = ArrayPool<int>.Shared.Rent(100); return arr; } ");  // утечка
    Console.WriteLine();

    // 7. Большие данные
    Console.WriteLine("7. Обработка большого объёма данных:");
    void ProcessLargeData(int size)
    {
        int[] buffer = ArrayPool<int>.Shared.Rent(size);
        try
        {
            for (int i = 0; i < size; i++) buffer[i] = i;
            Console.WriteLine($"Обработано {size} элементов (длина буфера {buffer.Length})");
        }
        finally { ArrayPool<int>.Shared.Return(buffer); }
    }
    ProcessLargeData(1000000);
}

await RunExamples();