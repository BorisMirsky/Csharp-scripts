#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


// - lock – синтаксический сахар над Monitor.Enter / Monitor.Exit.
// - Используется для защиты общего ресурса от одновременного доступа из нескольких потоков.
// - Важно: объект блокировки должен быть ссылочным типом и доступен всем потокам.
// - Monitor.TryEnter(lockObj, timeout) – попытка захватить блокировку с таймаутом.
// - Monitor.Wait / Monitor.Pulse – для сигнализации между потоками (не рассматриваем здесь).
// - Deadlock возникает при взаимной блокировке двух потоков.

// Что демонстрируется:
// Race condition – два потока одновременно инкрементируют счётчик без синхронизации, результат меньше ожидаемого.
// Исправление через lock – счётчик становится корректным.
// Monitor.TryEnter с таймаутом – один поток не может захватить блокировку и выполняет альтернативное действие.
// Защита коллекции – List<T> с lock при параллельном добавлении.
// sMonitor.Enter/Exit напрямую – эквивалент lock, но с явной обработкой lockTaken.


// 1. Пример гонки (race condition) без синхронизации
Console.WriteLine("1. Гонка (race condition) без синхронизации:");

int counter = 0;
void IncrementWithoutLock()
{
    for (int i = 0; i < 100000; i++)
        counter++; // неатомарная операция!
}

Thread t1 = new Thread(IncrementWithoutLock);
Thread t2 = new Thread(IncrementWithoutLock);
t1.Start();
t2.Start();
t1.Join();
t2.Join();

Console.WriteLine($"   Ожидалось 200000, получено {counter} (обычно меньше из-за гонки)");
Console.WriteLine();




// 2. Исправление через lock
Console.WriteLine("2. Исправление через lock:");

int counterLocked = 0;
object lockObj = new object();

void IncrementWithLock()
{
    for (int i = 0; i < 100000; i++)
    {
        lock (lockObj)
        {
            counterLocked++;
        }
    }
}

Thread t3 = new Thread(IncrementWithLock);
Thread t4 = new Thread(IncrementWithLock);
t3.Start();
t4.Start();
t3.Join();
t4.Join();

Console.WriteLine($"   Результат с lock: {counterLocked} (корректно)");
Console.WriteLine();




// 3. Monitor.TryEnter с таймаутом
Console.WriteLine("3. Monitor.TryEnter с таймаутом:");

object lockObj2 = new object();
int sharedResource = 0;

void TryEnterExample(int timeoutMs)
{
    if (Monitor.TryEnter(lockObj2, timeoutMs))
    {
        try
        {
            // Имитация работы с ресурсом
            Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} захватил блокировку");
            sharedResource++;
            Thread.Sleep(100); // работа
        }
        finally
        {
            Monitor.Exit(lockObj2);
            Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} освободил блокировку");
        }
    }
    else
    {
        Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} не смог захватить блокировку за {timeoutMs} мс");
    }
}

// Запускаем два потока: один захватывает блокировку надолго, второй пытается с таймаутом
Thread threadA = new Thread(() =>
{
    lock (lockObj2)
    {
        Console.WriteLine($"   Поток {Thread.CurrentThread.ManagedThreadId} захватил блокировку и спит 500 мс");
        Thread.Sleep(500);
    }
});
Thread threadB = new Thread(() => TryEnterExample(100)); // таймаут 100 мс
Thread threadC = new Thread(() => TryEnterExample(1000)); // таймаут 1 сек

threadA.Start();
Thread.Sleep(50); // даём threadA захватить блокировку
threadB.Start();
threadC.Start();

threadA.Join();
threadB.Join();
threadC.Join();
Console.WriteLine();




// 4. Использование lock для защиты коллекции
Console.WriteLine("4. Защита коллекции через lock:");

List<int> list = new List<int>();
object listLock = new object();

void AddToList(int value)
{
    lock (listLock)
    {
        list.Add(value);
    }
}

Parallel.For(0, 1000, i => AddToList(i));
Console.WriteLine($"   Размер списка: {list.Count} (должно быть 1000)");
Console.WriteLine();




// 5. Использование Monitor.Enter/Exit напрямую (без lock)

Console.WriteLine("5. Monitor.Enter/Exit напрямую (эквивалент lock):");

int directCounter = 0;
object directLock = new object();

void DirectIncrement()
{
    for (int i = 0; i < 10000; i++)
    {
        bool lockTaken = false;
        try
        {
            Monitor.Enter(directLock, ref lockTaken);
            directCounter++;
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(directLock);
        }
    }
}

Thread t5 = new Thread(DirectIncrement);
Thread t6 = new Thread(DirectIncrement);
t5.Start();
t6.Start();
t5.Join();
t6.Join();

Console.WriteLine($"   Прямой Monitor.Enter/Exit: {directCounter} (корректно)");
Console.WriteLine();

