using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// ==================================================================
// Memory<T> и ReadOnlyMemory<T>
// 
// 1. Span<T> — стэковый тип (ref struct). Он быстр, но:
//    - не может быть полем класса
//    - не может использоваться в async/await
//    - не может быть в замыканиях (лямбдах, итераторах)
//
// 2. Memory<T> — ссылочный тип (struct, но не ref struct). Он:
//    - может храниться в полях класса, массивах
//    - может использоваться в async/await
//    - поддерживает лямбды и итераторы
//
// 3. Memory<T> — это "ленивый" контейнер: он может указывать на:
//    - массив T[]
//    - строку (ReadOnlyMemory<char>)
//    - управляемую память (MemoryManager<T>)
//    - пул массивов (ArrayPool<T>)
//
// 4. Из Memory<T> можно получить Span<T> через .Span (но осторожно: Span, полученный из Memory, нельзя держать при переходе через await)
//
// 5. ReadOnlyMemory<T> — readonly версия, часто используется для строк (ReadOnlyMemory<char>)
//
// 6. Основной сценарий: асинхронная обработка буферов, работа с пулами памяти
// ==================================================================

async Task RunMemoryExamples()
{
    Console.WriteLine("=== Memory<T> и ReadOnlyMemory<T> демонстрация ===\n");
    

    // 1. Создание Memory<T> из массива
    int[] array = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    Memory<int> memory = array.AsMemory();
    Memory<int> slice = memory.Slice(2, 5); // элементы 3..7
    
    Console.WriteLine($"Исходный массив: [{string.Join(",", array)}]");
    Console.WriteLine($"Срез Memory: [{string.Join(",", slice.ToArray())}]");
    
    // Модификация через Span (получаем Span из Memory)
    Span<int> spanFromMemory = slice.Span;
    spanFromMemory[0] = 100;
    spanFromMemory[3] = 200;
    Console.WriteLine($"После изменения через Span: [{string.Join(",", array)}]\n");
    

    // 2. Отличие от Span: Memory можно использовать в async/await
    async Task<int> ProcessBufferAsync(Memory<byte> buffer, int offset)
    {
        await Task.Delay(10); // имитация async работы
        var span = buffer.Span; // получаем Span только внутри синхронного участка
        return span[offset];
    }
    
    byte[] byteData = new byte[256];
    var mem = byteData.AsMemory();
    var result = await ProcessBufferAsync(mem, 42);
    Console.WriteLine($"Асинхронная обработка Memory: result = {result}");
    

    // 3. ReadOnlyMemory<T> — неизменяемое представление
    string text = "Hello, world!";
    ReadOnlyMemory<char> readOnlyChars = text.AsMemory();
    // readOnlyChars.Span[0] = 'h'; // Ошибка компиляции: readonly
    
    ReadOnlySpan<char> sliceOfText = readOnlyChars.Span.Slice(7, 5);
    Console.WriteLine($"ReadOnlyMemory<char> из строки: '{sliceOfText.ToString()}'\n");
    

    // 4. Использование Memory<T> с ArrayPool — типичный zero-allocation паттерн
    byte[] rented = ArrayPool<byte>.Shared.Rent(1024);
    try
    {
        var memoryFromPool = rented.AsMemory(0, 1024);
        memoryFromPool.Span.Fill(0xAB); // заполняем данными
        
        // Определяем асинхронную функцию для записи в поток
        async Task WriteToStreamAsync(Stream stream, ReadOnlyMemory<byte> data)
        {
            await stream.WriteAsync(data);
        }
        
        // Используем её с MemoryStream
        using (var ms = new MemoryStream())
        {
            await WriteToStreamAsync(ms, memoryFromPool);
            Console.WriteLine($"Записано в MemoryStream через WriteToStreamAsync: {ms.Length} байт");
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(rented);
    }
    Console.WriteLine();
    

    // 5. Преобразование Memory в Span и обратно (осторожно)
    int[] numbers = Enumerable.Range(0, 100).ToArray();
    Memory<int> memNumbers = numbers.AsMemory();
    
    Span<int> localSpan = memNumbers.Span;
    localSpan[10] = -1;
    
    var changed = memNumbers.Slice(10, 1).ToArray();
    Console.WriteLine($"Изменённый элемент через Span: {changed[0]}\n");
    

    // 6. Пример: асинхронное чтение данных с использованием Memory<T> (имитация)
    async Task<ReadOnlyMemory<byte>> ReadDataAsync()
    {
        byte[] data = new byte[100];
        for (int i = 0; i < data.Length; i++) data[i] = (byte)i;
        await Task.Delay(1);
        return data.AsMemory();
    }
    
    var asyncData = await ReadDataAsync();
    Console.WriteLine($"Асинхронно прочитано {asyncData.Length} байт; первые 5: {string.Join(",", asyncData.Span.Slice(0,5).ToArray())}");
}

// Запуск
await RunMemoryExamples();