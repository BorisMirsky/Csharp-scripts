#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;


// - Deadlock (взаимная блокировка) возникает, когда два или более потоков
//   удерживают блокировки и ждут друг друга, чтобы освободить их.
// - Классический пример: два потока захватывают два объекта в разном порядке.
// - Способы предотвращения:
//   1. Всегда захватывать блокировки в одном и том же порядке (глобальный порядок).
//   2. Использовать Monitor.TryEnter с таймаутом (неблокирующий захват).
//   3. Использовать более высокоуровневые примитивы (например, SemaphoreSlim, AsyncLock).


// Что демонстрируется
// Классический deadlock – два потока захватывают замки в обратном порядке. Программа останавливается, пока вы не нажмёте клавишу (показываем, что потоки зависли).
// Предотвращение через единый порядок – все потоки захватывают замки в одинаковой последовательности – deadlock не возникает.
// Предотвращение через Monitor.TryEnter – потоки пытаются захватить замки с таймаутом, и если не удаётся – выходят, избегая deadlock.
// Дополнительный пример с тремя потоками – демонстрирует, что deadlock может возникнуть даже при большем числе ресурсов; здесь может повезти, но это не гарантировано.
//     Если зависли то выходим ctrl+C


// 1. Классический deadlock с двумя замками
Console.WriteLine("1. Демонстрация deadlock (два замка, разный порядок):");

object lockA = new object();
object lockB = new object();

// Функция, которая захватывает lockA, затем lockB
void Method1()
{
    lock (lockA)
    {
        Console.WriteLine("   Поток 1 захватил lockA, ждёт lockB...");
        Thread.Sleep(100); // имитация работы, чтобы второй поток успел захватить lockB
        lock (lockB)
        {
            Console.WriteLine("   Поток 1 захватил lockB (никогда не случится)");
        }
    }
}

// Функция, которая захватывает lockB, затем lockA (обратный порядок)
void Method2()
{
    lock (lockB)
    {
        Console.WriteLine("   Поток 2 захватил lockB, ждёт lockA...");
        Thread.Sleep(100);
        lock (lockA)
        {
            Console.WriteLine("   Поток 2 захватил lockA (никогда не случится)");
        }
    }
}

Console.WriteLine("   Запускаем два потока, которые создадут deadlock...");
Thread t1 = new Thread(Method1);
Thread t2 = new Thread(Method2);
t1.Start();
t2.Start();

// Даём потокам время войти в deadlock
Thread.Sleep(500);

// Проверяем, живы ли потоки (они должны быть заблокированы)
Console.WriteLine($"   Поток 1 жив: {t1.IsAlive}, Поток 2 жив: {t2.IsAlive}");
Console.WriteLine("   (Они зависли в deadlock. Нажмите любую клавишу для продолжения...)");
Console.ReadKey();
Console.WriteLine();


// 2. Предотвращение deadlock: единый порядок блокировок
Console.WriteLine("2. Предотвращение: всегда захватывать замки в одинаковом порядке.");

object lockC = new object();
object lockD = new object();

void SafeMethod1()
{
    // Фиксируем порядок: всегда lockC, затем lockD
    lock (lockC)
    {
        Console.WriteLine("   SafeMethod1: захватил lockC");
        Thread.Sleep(50);
        lock (lockD)
        {
            Console.WriteLine("   SafeMethod1: захватил lockD");
        }
    }
}

void SafeMethod2()
{
    // Тот же порядок: lockC, затем lockD
    lock (lockC)
    {
        Console.WriteLine("   SafeMethod2: захватил lockC");
        Thread.Sleep(50);
        lock (lockD)
        {
            Console.WriteLine("   SafeMethod2: захватил lockD");
        }
    }
}

Thread t3 = new Thread(SafeMethod1);
Thread t4 = new Thread(SafeMethod2);
t3.Start();
t4.Start();
t3.Join();
t4.Join();
Console.WriteLine("   Оба потока успешно завершились (без deadlock)\n");


// 3. Предотвращение deadlock через Monitor.TryEnter (таймаут)
Console.WriteLine("3. Предотвращение через Monitor.TryEnter (неблокирующий захват):");

object lockE = new object();
object lockF = new object();

void TryEnterMethod1()
{
    bool lockETaken = false;
    bool lockFTaken = false;
    try
    {
        // Пытаемся захватить lockE с таймаутом
        Monitor.TryEnter(lockE, 200, ref lockETaken);
        if (!lockETaken)
        {
            Console.WriteLine("   TryEnterMethod1: не удалось захватить lockE, выходим.");
            return;
        }
        Console.WriteLine("   TryEnterMethod1: захватил lockE");
        Thread.Sleep(50);

        Monitor.TryEnter(lockF, 200, ref lockFTaken);
        if (!lockFTaken)
        {
            Console.WriteLine("   TryEnterMethod1: не удалось захватить lockF, выходим.");
            return;
        }
        Console.WriteLine("   TryEnterMethod1: захватил lockF");
        // Работаем с ресурсами...
    }
    finally
    {
        if (lockFTaken) Monitor.Exit(lockF);
        if (lockETaken) Monitor.Exit(lockE);
    }
}

void TryEnterMethod2()
{
    bool lockFTaken = false;
    bool lockETaken = false;
    try
    {
        Monitor.TryEnter(lockF, 200, ref lockFTaken);
        if (!lockFTaken)
        {
            Console.WriteLine("   TryEnterMethod2: не удалось захватить lockF, выходим.");
            return;
        }
        Console.WriteLine("   TryEnterMethod2: захватил lockF");
        Thread.Sleep(50);

        Monitor.TryEnter(lockE, 200, ref lockETaken);
        if (!lockETaken)
        {
            Console.WriteLine("   TryEnterMethod2: не удалось захватить lockE, выходим.");
            return;
        }
        Console.WriteLine("   TryEnterMethod2: захватил lockE");
    }
    finally
    {
        if (lockETaken) Monitor.Exit(lockE);
        if (lockFTaken) Monitor.Exit(lockF);
    }
}

Thread t5 = new Thread(TryEnterMethod1);
Thread t6 = new Thread(TryEnterMethod2);
t5.Start();
t6.Start();
t5.Join();
t6.Join();
Console.WriteLine("   TryEnter предотвратил deadlock – один или оба потока вышли по таймауту.\n");


// 4. Продвинутый пример: deadlock с использованием нескольких ресурсов (классический «философы»)
Console.WriteLine("4. Дополнительно: пример с трёмя замками (для наглядности)");

object resource1 = new object();
object resource2 = new object();
object resource3 = new object();

void DangerousMethod(int id)
{
    // Опасный порядок: в зависимости от id меняем порядок
    object first, second;
    if (id % 2 == 0)
    {
        first = resource1;
        second = resource2;
    }
    else
    {
        first = resource2;
        second = resource1;
    }
    lock (first)
    {
        Console.WriteLine($"   Поток {id} захватил {first.GetHashCode()}");
        Thread.Sleep(50);
        lock (second)
        {
            Console.WriteLine($"   Поток {id} захватил {second.GetHashCode()}");
        }
    }
}

Console.WriteLine("   Запускаем 3 потока с разным порядком, может возникнуть deadlock.");
Thread[] threads = new Thread[3];
for (int i = 0; i < 3; i++)
{
    int idx = i;
    threads[i] = new Thread(() => DangerousMethod(idx));
}
foreach (var t in threads) t.Start();
foreach (var t in threads) t.Join(); // Здесь может быть deadlock – программа зависнет, если он произойдёт.
Console.WriteLine("   Все потоки завершились (повезло, но полагаться на это нельзя).\n");



