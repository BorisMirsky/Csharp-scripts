#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;




// - GC.GetTotalAllocatedBytes(true) – возвращает общее количество байт,
//   выделенных в управляемой куче с момента запуска процесса.
//   Параметр true вызывает принудительную сборку мусора перед измерением,
//   чтобы получить стабильную точку отсчёта.
// - Разница между двумя вызовами показывает объём аллокаций, произошедших
//   между ними (включая временные объекты).
// - GC.CollectionCount(0/1/2) – показывает сколько раз выполнялась сборка
//   мусора для каждого поколения (0 – молодое, 1 – среднее, 2 – старое).
// - Измерение аллокаций полезно для выявления участков кода, создающих
//   много временных объектов и нагружающих GC.


// Что демонстрируется
// Базовое измерение аллокаций через GC.GetTotalAllocatedBytes(true).
// Сравнение аллокаций при конкатенации строк (через += и StringBuilder).
// Измерение количества сборок GC для поколений (0, 1, 2).
// Измерение аллокаций без принудительной сборки (менее точно, но быстрее).
// Сравнение «тяжёлого» и «лёгкого» методов по аллокациям.
// Рекомендации по уменьшению аллокаций.


// 1. Базовое измерение аллокаций
Console.WriteLine("1. Базовое измерение аллокаций:");

// Функция для замера аллокаций
long MeasureAllocations(Action action)
{
    // Принудительно собираем мусор и получаем точку отсчёта
    long startBytes = GC.GetTotalAllocatedBytes(true);
    
    action(); // выполняем измеряемый код
    
    // Снова принудительно собираем и получаем конечное значение
    long endBytes = GC.GetTotalAllocatedBytes(true);
    
    return endBytes - startBytes;
}

// Тестируем на разных операциях
void AllocateArray()
{
    int[] arr = new int[10000];
    for (int i = 0; i < arr.Length; i++) arr[i] = i;
}

long arrayAllocs = MeasureAllocations(AllocateArray);
Console.WriteLine($"   Аллокации при создании int[10000]: {arrayAllocs} байт");

// Строки
long stringAllocs = MeasureAllocations(() =>
{
    string s = "";
    for (int i = 0; i < 100; i++)
        s += "a"; // создаёт много промежуточных строк
});
Console.WriteLine($"   Аллокации при конкатенации строк (100 раз): {stringAllocs} байт");

// LINQ
long linqAllocs = MeasureAllocations(() =>
{
    var list = Enumerable.Range(0, 1000).ToList();
    var filtered = list.Where(x => x % 2 == 0).ToArray();
});
Console.WriteLine($"   Аллокации при LINQ (ToList + Where + ToArray): {linqAllocs} байт");
Console.WriteLine();


// 2. Сравнение аллокаций при разных способах конкатенации строк
Console.WriteLine("2. Сравнение конкатенации строк:");

long concatAllocs = MeasureAllocations(() =>
{
    string s = "";
    for (int i = 0; i < 100; i++)
        s += i.ToString();
});
Console.WriteLine($"   Конкатенация через += : {concatAllocs} байт");

long builderAllocs = MeasureAllocations(() =>
{
    var sb = new StringBuilder();
    for (int i = 0; i < 100; i++)
        sb.Append(i);
    string s = sb.ToString();
});
Console.WriteLine($"   StringBuilder: {builderAllocs} байт");
Console.WriteLine($"   (StringBuilder значительно меньше аллоцирует, т.к. не создаёт промежуточные строки)");
Console.WriteLine();


// 3. Измерение количества сборок GC (поколения)
Console.WriteLine("3. Измерение количества сборок GC:");

// Функция для измерения сборок
void MeasureGCCounts(Action action)
{
    int gen0 = GC.CollectionCount(0);
    int gen1 = GC.CollectionCount(1);
    int gen2 = GC.CollectionCount(2);

    action();

    int gen0After = GC.CollectionCount(0);
    int gen1After = GC.CollectionCount(1);
    int gen2After = GC.CollectionCount(2);

    Console.WriteLine($"   Gen0: {gen0After - gen0} сборок");
    Console.WriteLine($"   Gen1: {gen1After - gen1} сборок");
    Console.WriteLine($"   Gen2: {gen2After - gen2} сборок");
}

Console.WriteLine("   Создание 100000 объектов с вызовом GC.Collect (демонстрация):");
MeasureGCCounts(() =>
{
    for (int i = 0; i < 100000; i++)
    {
        var obj = new object();
        if (i % 1000 == 0)
            GC.Collect(); // принудительная сборка для демонстрации
    }
});
Console.WriteLine("   (Принудительные сборки могут искажать реальную картину, но показывают механизм)");
Console.WriteLine();


// 4. Реальное измерение аллокаций без принудительной сборки (осторожно)
Console.WriteLine("4. Измерение аллокаций без принудительной сборки (менее точно):");

long MeasureAllocationsSoft(Action action)
{
    long start = GC.GetTotalAllocatedBytes(false); // без принудительной сборки
    action();
    long end = GC.GetTotalAllocatedBytes(false);
    return end - start;
}

long softAllocs = MeasureAllocationsSoft(() =>
{
    var list = new List<int>(10000);
    for (int i = 0; i < 10000; i++) list.Add(i);
});
Console.WriteLine($"   Аллокации List<int>(10000) (без точной сборки): {softAllocs} байт");
Console.WriteLine("   (Может быть неточно, т.к. не учитывает объекты, ожидающие сборки)");
Console.WriteLine();


// 5. Демонстрация влияния аллокаций на производительность
Console.WriteLine("5. Влияние аллокаций на производительность:");

void HeavyAllocMethod()
{
    var list = new List<byte[]>();
    for (int i = 0; i < 100; i++)
        list.Add(new byte[1000]);
}

void LightAllocMethod()
{
    int sum = 0;
    for (int i = 0; i < 100000; i++)
        sum += i;
}

long heavyAllocs = MeasureAllocations(HeavyAllocMethod);
long lightAllocs = MeasureAllocations(LightAllocMethod);

Console.WriteLine($"   Тяжёлый по аллокациям метод: {heavyAllocs} байт");
Console.WriteLine($"   Лёгкий по аллокациям метод: {lightAllocs} байт");
Console.WriteLine($"   Разница в ~{heavyAllocs / (double)lightAllocs:F1} раз");
Console.WriteLine();


// 6. Рекомендации
Console.WriteLine("6. Рекомендации:");
Console.WriteLine("   Используйте GC.GetTotalAllocatedBytes(true) для точного измерения.");
Console.WriteLine("   Сравнивайте разные реализации по аллокациям.");
Console.WriteLine("   Минимизируйте создание временных объектов (строк, массивов, делегатов).");
Console.WriteLine("   Используйте StringBuilder для строк, ArrayPool для массивов, пулы объектов.");
Console.WriteLine("   Помните, что аллокации не равны производительности – иногда лучше выделить больше, но реже.");

