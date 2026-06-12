
// Span - типа массив, но без выделения дополнительной памяти.
// ReadOnlySpan - типа Span но для неизменяемых данных.

// Запуск: dotnet script span_demo.csx


using System;
using System.Linq;


void Demo()
{


// 1. Span<T> из массива: работа с фрагментом без копирования
Console.WriteLine("--- Span<T> из массива: работа с фрагментом без копирования---");
int[] numbers = Enumerable.Range(0, 10).ToArray();
Span<int> spanOfNumbers = numbers.AsSpan();

// Берем срез (slice) — без аллокации новой памяти
Span<int> slice = spanOfNumbers.Slice(2, 5);
Console.WriteLine($"Исходный массив: [{string.Join(", ", numbers)}]");
Console.WriteLine($"Срез (элементы 2..6): [{string.Join(", ", slice.ToArray())}]");

// Меняем элементы через срез — изменения видны в исходном массиве
slice[0] = 100;
slice[3] = 200;
Console.WriteLine($"После изменения через Span: [{string.Join(", ", numbers)}]\n");



// 2. Работа со строками без аллокации (ReadOnlySpan<char>)
string text = "Hello, World! How are you?";
ReadOnlySpan<char> textSpan = text.AsSpan();

// Извлекаем подстроку без выделения новой строки
ReadOnlySpan<char> worldSpan = textSpan.Slice(7, 5); // "World"
Console.WriteLine($"Исходная строка: '{text}'");
Console.WriteLine($"Срез (без аллокации строки): '{worldSpan.ToString()}'");

// Поиск символа и срез
int commaPos = textSpan.IndexOf(',');
if (commaPos != -1)
{
    ReadOnlySpan<char> beforeComma = textSpan.Slice(0, commaPos);
    Console.WriteLine($"Часть до запятой (без аллокации): '{beforeComma.ToString()}'\n");
}




// 3. Конвертация между Span и массивами без копирования
byte[] byteArray = new byte[100];
Span<byte> byteSpan = byteArray.AsSpan();

// Заполняем Span
byteSpan.Fill(0xFF);
Console.WriteLine($"Байтовый массив заполнен: byteArray[0] = {byteArray[0]}, [50] = {byteArray[50]}");



// 4. Span как возвращаемое значение (нельзя вернуть Span из метода, указывающий на стек)
// Но можно вернуть Span на управляемый массив, если метод не async и не итератор
static Span<int> GetSliceOfArray(Span<int> source, int start, int length)
{
    // Проверка границ (можно опустить для производительности, но для примера)
    //if (start < 0 || length < 0 || start + length > source.Length)
    //    throw new ArgumentOutOfRangeException();
    return source.Slice(start, length);
}


int[] bigArray = Enumerable.Range(100, 20).ToArray();
Span<int> sliceFromMethod = GetSliceOfArray(bigArray.AsSpan(), 5, 5);
Console.WriteLine($"Срез, полученный из метода: [{string.Join(", ", sliceFromMethod.ToArray())}]");
Console.WriteLine();




// 5. Span и stackalloc (безопасное выделение на стеке)
Span<int> stackSpan = stackalloc int[5] { 10, 20, 30, 40, 50 };
Console.WriteLine($"stackalloc Span: [{string.Join(", ", stackSpan.ToArray())}]");
stackSpan[2] = 999;
Console.WriteLine($"После изменения: [{string.Join(", ", stackSpan.ToArray())}]");
Console.WriteLine();





// 6. Сравнение с традиционным подходом (копирование)
Console.WriteLine("--- Сравнение производительности (концептуально) ---");
Console.WriteLine("Традиционный подход: new[] + CopyTo = аллокация нового массива.");
Console.WriteLine("Подход со Span: AsSpan().Slice() = 0 аллокаций, работа с исходными данными.");
Console.WriteLine("\nИтог: Span позволяет безопасно и эффективно работать с фрагментами памяти");
Console.WriteLine("без создания копий, снижая нагрузку на GC.");

}


Demo();

