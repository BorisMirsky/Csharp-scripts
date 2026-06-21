#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


// - ConcurrentQueue<T> – потокобезопасная очередь (FIFO).
// - ConcurrentStack<T> – потокобезопасный стек (LIFO).
// - ConcurrentBag<T> – неупорядоченная коллекция, оптимизированная для сценариев, где каждый поток пишет и читает в основном свои данные.
// - ConcurrentDictionary<TKey, TValue> – потокобезопасный словарь с атомарными операциями (AddOrUpdate, GetOrAdd).
// - BlockingCollection<T> – обёртка над IProducerConsumerCollection, поддерживает блокирующие операции (Take, Add) и ограничение ёмкости.
// - Все коллекции используют lock-free или мелкогранулярные блокировки для высокой производительности.



// Что демонстрируется:
// ConcurrentQueue – producer-consumer с TryDequeue.
// ConcurrentStack – LIFO с Push / TryPop.
// ConcurrentBag – неупорядоченное добавление и извлечение.
// ConcurrentDictionary – AddOrUpdate, GetOrAdd (атомарные операции).
// BlockingCollection – ограниченная очередь с блокировкой при заполнении, CompleteAdding, GetConsumingEnumerable.
// Сравнение с обычными коллекциями (текстовое пояснение).



// 1. ConcurrentQueue – производитель-потребитель
// -------------------------------------------------------------
Console.WriteLine("1. ConcurrentQueue (FIFO):");

ConcurrentQueue<int> queue = new ConcurrentQueue<int>();

// Producer
Task producerQueue = Task.Run(() =>
{
    for (int i = 0; i < 100; i++)
    {
        queue.Enqueue(i);
        Thread.Sleep(1); // имитация работы
    }
});

// Consumer
Task consumerQueue = Task.Run(() =>
{
    int sum = 0;
    int count = 0;
    while (count < 100)
    {
        if (queue.TryDequeue(out int item))
        {
            sum += item;
            count++;
        }
        else
            Thread.Sleep(2);
    }
    Console.WriteLine($"   Сумма элементов (ожидалось 4950): {sum}");
});

Task.WaitAll(producerQueue, consumerQueue);
Console.WriteLine();


// 2. ConcurrentStack (LIFO)
Console.WriteLine("2. ConcurrentStack (LIFO):");

ConcurrentStack<int> stack = new ConcurrentStack<int>();

Parallel.For(0, 100, i => stack.Push(i));

int[] items = new int[100];
for (int i = 0; i < 100; i++)
    stack.TryPop(out items[i]);

Console.WriteLine($"   Первые 10 извлечённых элементов (должны быть 99..90): {string.Join(", ", items.Take(10))}");
Console.WriteLine();


// 3. ConcurrentBag – неупорядоченная коллекция
Console.WriteLine("3. ConcurrentBag (неупорядоченная):");

ConcurrentBag<int> bag = new ConcurrentBag<int>();

Parallel.For(0, 1000, i => bag.Add(i));

int bagSum = 0;
while (bag.TryTake(out int item))
    bagSum += item;

Console.WriteLine($"   Сумма элементов (ожидалось 499500): {bagSum}");
Console.WriteLine();


// 4. ConcurrentDictionary – атомарные операции
Console.WriteLine("4. ConcurrentDictionary (AddOrUpdate, GetOrAdd):");

ConcurrentDictionary<string, int> dict = new ConcurrentDictionary<string, int>();

// Параллельное добавление с агрегацией
Parallel.For(0, 1000, i =>
{
    string key = (i % 10).ToString(); // 10 ключей, каждый будет обновлён 100 раз
    dict.AddOrUpdate(key, 1, (k, v) => v + 1);
});

Console.WriteLine("   Итоговые значения по ключам:");
foreach (var kv in dict.OrderBy(x => x.Key))
    Console.WriteLine($"      {kv.Key}: {kv.Value} (ожидается 100)");

// GetOrAdd – потокобезопасное получение или добавление
string newKey = "newKey";
int value = dict.GetOrAdd(newKey, 42);
Console.WriteLine($"   GetOrAdd для {newKey}: {value} (создано)");

int existing = dict.GetOrAdd("0", 999);
Console.WriteLine($"   GetOrAdd для '0' (уже существует): {existing} (старое значение)");
Console.WriteLine();


// 5. BlockingCollection – блокирующая очередь с ограничением ёмкости
Console.WriteLine("5. BlockingCollection (producer-consumer с блокировкой):");

BlockingCollection<int> blockingQueue = new BlockingCollection<int>(boundedCapacity: 5);

// Producer
Task producerBlocking = Task.Run(() =>
{
    for (int i = 0; i < 20; i++)
    {
        blockingQueue.Add(i);
        Console.WriteLine($"   Producer: добавлен {i} (текущий размер ~{blockingQueue.Count})");
        Thread.Sleep(50);
    }
    blockingQueue.CompleteAdding(); // сигнал о завершении
});

// Consumer
Task consumerBlocking = Task.Run(() =>
{
    int sum = 0;
    foreach (var item in blockingQueue.GetConsumingEnumerable())
    {
        sum += item;
        Console.WriteLine($"   Consumer: извлечён {item}");
        Thread.Sleep(100); // медленный потребитель, чтобы показать блокировку
    }
    Console.WriteLine($"   Сумма всех элементов: {sum} (ожидалось 190)");
});

Task.WaitAll(producerBlocking, consumerBlocking);
Console.WriteLine();


// 6. Сравнение с обычной коллекцией + lock (концептуально)
Console.WriteLine("6. Сравнение производительности (без бенчмарка):");
Console.WriteLine("   ConcurrentQueue обычно быстрее, чем List<T> + lock, в высоконагруженных сценариях.");
Console.WriteLine("   ConcurrentDictionary эффективнее Dictionary + lock при частых чтениях/записях.");
Console.WriteLine("   BlockingCollection удобна для организации producer-consumer с ограничением.");
Console.WriteLine();

