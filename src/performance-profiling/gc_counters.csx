#nullable disable

using System;
using System.Collections.Generic;


// - GC использует поколения (Gen0, Gen1, Gen2). Новые объекты попадают в Gen0,
//   сборки Gen0 происходят часто и быстро. Выжившие переходят в Gen1, затем в Gen2.
// - Gen2 – самые долгоживущие объекты. Сборки Gen2 дорогие (full GC).
// - Большие объекты (>85000 байт) попадают в LOH (Large Object Heap) и собираются реже.
// - GC.GetCollectionCount(0/1/2) – количество сборок каждого поколения с начала процесса.
// - GC.GetTotalMemory – общий объём памяти, выделенной в управляемой куче.
// - GC.GetGCMemoryInfo – детальная информация о последней сборке (время, память, фрагментация).
// - GC.TryStartNoGCRegion – попытка отключить сборки на время (осторожно!).
// - Ручные вызовы GC.Collect – антипаттерн в продакшене, используют только для диагностики.



// 1. Текущие счётчики сборок
Console.WriteLine("1. Количество сборок GC с начала процесса:");

void PrintGCStats()
{
    Console.WriteLine($"   Gen0: {GC.CollectionCount(0)}");
    Console.WriteLine($"   Gen1: {GC.CollectionCount(1)}");
    Console.WriteLine($"   Gen2: {GC.CollectionCount(2)}");
    Console.WriteLine($"   Total memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
}

PrintGCStats();
Console.WriteLine();


// 2. Демонстрация повышения поколения объекта
Console.WriteLine("2. Повышение поколения объекта после сборок:");

object obj = new object();
int genBefore = GC.GetGeneration(obj);
Console.WriteLine($"   Объект до сборок: Gen{genBefore}");

GC.Collect(0);
GC.WaitForPendingFinalizers();
int genAfterGen0 = GC.GetGeneration(obj);
Console.WriteLine($"   После GC.Collect(0): Gen{genAfterGen0}");

GC.Collect(1);
GC.WaitForPendingFinalizers();
int genAfterGen1 = GC.GetGeneration(obj);
Console.WriteLine($"   После GC.Collect(1): Gen{genAfterGen1}");

GC.Collect(2);
GC.WaitForPendingFinalizers();
int genAfterGen2 = GC.GetGeneration(obj);
Console.WriteLine($"   После GC.Collect(2): Gen{genAfterGen2}");
Console.WriteLine("   (Объект пережил все сборки и достиг Gen2)");
Console.WriteLine();



// 3. Сборки GC при создании объектов
Console.WriteLine("3. Изменение счётчиков после создания тысяч объектов:");

long gen0Start = GC.CollectionCount(0);
long gen1Start = GC.CollectionCount(1);
long gen2Start = GC.CollectionCount(2);

for (int i = 0; i < 100000; i++)
{
    var temp = new byte[100];
}

long gen0End = GC.CollectionCount(0);
long gen1End = GC.CollectionCount(1);
long gen2End = GC.CollectionCount(2);

Console.WriteLine($"   Gen0 сборок до: {gen0Start}, после: {gen0End}, изменений: {gen0End - gen0Start}");
Console.WriteLine($"   Gen1 сборок до: {gen1Start}, после: {gen1End}, изменений: {gen1End - gen1Start}");
Console.WriteLine($"   Gen2 сборок до: {gen2Start}, после: {gen2End}, изменений: {gen2End - gen2Start}");
Console.WriteLine();



// 4. Информация о последней сборке GC (стабильные свойства)
Console.WriteLine("4. Информация о последней сборке GC (GC.GetGCMemoryInfo):");

GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
GC.WaitForPendingFinalizers();

GCMemoryInfo info = GC.GetGCMemoryInfo();
Console.WriteLine($"   Индекс сборки: {info.Index}");
Console.WriteLine($"   Поколение: Gen{info.Generation}");
Console.WriteLine($"   Компактинг: {(info.Compacted ? "Да" : "Нет")}");
Console.WriteLine($"   Общий объём памяти до сборки (байт): {info.TotalAvailableMemoryBytes / 1024 / 1024} MB");
Console.WriteLine("   (Дополнительные свойства, такие как FreedBytes, PauseDurations, зависят от версии .NET)");
Console.WriteLine();



// 5. Демонстрация LOH (Large Object Heap)
Console.WriteLine("5. Большие объекты (>85000 байт) попадают в LOH:");

int genBeforeLOH = GC.GetGeneration(new byte[100000]);
Console.WriteLine($"   Поколение для массива 100000 байт: Gen{genBeforeLOH} (обычно Gen2 или LOH)");
Console.WriteLine("   (LOH не имеет собственного поколения, считается частью Gen2)");
Console.WriteLine();



// 6. GC.TryStartNoGCRegion – запрет сборок на время
Console.WriteLine("6. GC.TryStartNoGCRegion (запрет сборок):");

long initialGen0 = GC.CollectionCount(0);
bool success = GC.TryStartNoGCRegion(10 * 1024 * 1024);
if (success)
{
    Console.WriteLine("   Режим NoGCRegion активирован");
    var list = new List<int>();
    for (int i = 0; i < 100000; i++)
        list.Add(i);
    GC.EndNoGCRegion();
    long gen0After = GC.CollectionCount(0);
    Console.WriteLine($"   Сборок Gen0 во время NoGCRegion: {gen0After - initialGen0} (должно быть 0)");
}
else
{
    Console.WriteLine("   Не удалось войти в режим NoGCRegion (возможно, память превышена)");
}
Console.WriteLine();



// 7. Рекомендации
Console.WriteLine("7. Рекомендации:");
Console.WriteLine("   Избегайте ручных вызовов GC.Collect в продакшене.");
Console.WriteLine("   GC.TryStartNoGCRegion используйте только для критических участков.");
Console.WriteLine("   Для больших объектов рассмотрите пулы массивов (ArrayPool<T>).");
Console.WriteLine("   Следите за фрагментацией LOH через GC.GetGCMemoryInfo (если доступно).");

