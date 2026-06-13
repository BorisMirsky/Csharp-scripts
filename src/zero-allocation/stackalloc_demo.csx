#nullable disable
#pragma warning disable CS7022 // отключаем предупреждение о точке входа, пару раз возникало из ниоткуда

using System;

// ==================================================================
// stackalloc – выделение памяти на стеке
//
// 1. stackalloc выделяет блок памяти в стековом кадре (очень быстро, не требует GC).
// 2. Возвращает Span<T> (или unsafe указатель) – безопасный доступ без фиксации.
// 3. Живёт только внутри текущего метода; после возврата память автоматически освобождается.
// 4. Размер буфера должен быть известен во время компиляции или быть небольшим (по соображениям безопасности).
// 5. Используйте stackalloc для временных буферов небольшого размера (десятки/сотни байт).
// 6. Большие выделения на стеке могут вызвать StackOverflowException – всегда проверяйте.
// 7. Эффективно для высокопроизводительного кода: например, форматирование строк, шифрование, парсинг.
// 8. В C# 8+ stackalloc можно использовать внутри выражений (например, Span<int> s = stackalloc[] {1,2,3}).
// ==================================================================

void DemonstrateStackAlloc()
{

    // 1. Базовое выделение и заполнение
    Span<int> numbers = stackalloc int[5] { 10, 20, 30, 40, 50 };
    Console.WriteLine("1. stackalloc int[5] инициализирован:");
    Console.WriteLine($"   {string.Join(", ", numbers.ToArray())}\n");

    // 2. stackalloc без инициализатора (затем заполнение)
    Span<byte> buffer = stackalloc byte[10];
    for (int i = 0; i < buffer.Length; i++)
        buffer[i] = (byte)(i * 2);
    Console.WriteLine("2. Буфер байт заполнен в цикле:");
    Console.WriteLine($"   {string.Join(", ", buffer.ToArray())}\n");

    // 3. Выделение стека для временной строки (без аллокации в куче)
    static string FormatNumber(int value)
    {
        // Форматируем число как шестнадцатеричное без создания промежуточных строк в куче
        Span<char> chars = stackalloc char[8]; // максимум 8 символов для 32-битного int
        bool formatted = value.TryFormat(chars, out int written, "X");
        return formatted ? new string(chars.Slice(0, written)) : "?";
    }
    Console.WriteLine("3. Форматирование числа через stackalloc:");
    Console.WriteLine($"   255 -> {FormatNumber(255)}");
    Console.WriteLine($"   65535 -> {FormatNumber(65535)}\n");

    // 4. Сравнение производительности (концептуально)
    Console.WriteLine("4. Сравнение stackalloc vs new byte[]:");
    // stackalloc – на стеке, микросекунды
    Span<byte> stackBuffer = stackalloc byte[256];
    for (int i = 0; i < stackBuffer.Length; i++) stackBuffer[i] = (byte)i;
    // new byte[] – в куче, требует GC
    byte[] heapBuffer = new byte[256];
    for (int i = 0; i < heapBuffer.Length; i++) heapBuffer[i] = (byte)i;
    Console.WriteLine("   stackalloc: нет аллокации в куче, быстрее, автоматическое освобождение.");
    Console.WriteLine("   new byte[]: аллокация в куче, нагрузка на GC.\n");

    // 5. Использование stackalloc с небезопасным кодом (unsafe)
    unsafe void UnsafeExample()
    {
        int* ptr = stackalloc int[5];
        for (int i = 0; i < 5; i++)
            ptr[i] = i * i;
        Console.WriteLine("5. Unsafe: доступ через указатель:");
        Console.WriteLine($"   ptr[2] = {ptr[2]}\n");
    }
    UnsafeExample();

    // 6. Опасный случай: слишком большой буфер на стеке
    Console.WriteLine("6. Осторожно: чрезмерный stackalloc переполнит стек.");
    Console.WriteLine("   Например, stackalloc byte[100_000] скорее всего вызовет StackOverflowException.");
    Console.WriteLine("   Рекомендуемый порог – не более нескольких килобайт.\n");

    // 7. stackalloc внутри выражения (C# 8+)
    Span<int> inlineSpan = stackalloc[] { 1, 2, 3, 4, 5 }; // тип выводится
    Console.WriteLine("7. stackalloc[] с выводом типа:");
    Console.WriteLine($"   {string.Join(", ", inlineSpan.ToArray())}\n");

    // 8. Использование stackalloc с ReadOnlySpan
    ReadOnlySpan<char> charSpan = stackalloc char[] { 'A', 'B', 'C', 'D' };
    Console.WriteLine("8. ReadOnlySpan<char> из stackalloc:");
    Console.WriteLine($"   {charSpan.ToString()}\n");

    // 9. Пример: безопасная обработка данных без аллокаций
    static int SumPositive(Span<int> data)
    {
        int sum = 0;
        foreach (int val in data)
            if (val > 0) sum += val;
        return sum;
    }
    Span<int> stackData = stackalloc int[] { -5, 10, -3, 7, 0, 12 };
    int result = SumPositive(stackData);
    Console.WriteLine("9. Вычисление суммы положительных чисел из stackalloc:");
    Console.WriteLine($"   {string.Join(", ", stackData.ToArray())} -> {result}\n");

}

DemonstrateStackAlloc();