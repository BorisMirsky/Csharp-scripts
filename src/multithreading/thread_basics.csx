#nullable disable

using System;
using System.Threading;

// ==================================================================
// РАЗДЕЛ: Создание и запуск потоков (Thread)
// ==================================================================
// Теория коротко:
// - Поток (Thread) — наименьшая единица выполнения, планируемая ОС.
// - По умолчанию при запуске приложения создаётся один основной поток (Main).
// - Новые потоки создаются через new Thread(...) и запускаются через .Start().
// - При завершении основного потока завершаются и все фоновые (background) потоки,
//   а пользовательские (foreground) продолжают работу. По умолчанию поток — пользовательский.
// - Thread.Sleep(milliseconds) — приостанавливает поток (не рекомендуется для синхронизации,
//   но полезен в демонстрациях).
// - Thread.Join() — блокирует текущий поток до завершения указанного.
// ==================================================================


// -------------------------------------------------------------
// 1. Простейший поток с методом без параметров
// -------------------------------------------------------------
void SimpleMethod()
{
    Console.WriteLine($"   Простой поток: ID = {Thread.CurrentThread.ManagedThreadId}");
}

Thread thread1 = new Thread(SimpleMethod);
thread1.Start();
thread1.Join(); // дожидаемся завершения, чтобы вывод не смешивался
Console.WriteLine("1. Поток выполнил SimpleMethod\n");

// -------------------------------------------------------------
// 2. Поток с параметром (ParameterizedThreadStart)
// -------------------------------------------------------------
void MethodWithParameter(object state)
{
    string message = (string)state;
    Console.WriteLine($"   Поток с параметром (ID {Thread.CurrentThread.ManagedThreadId}): {message}");
}

Thread thread2 = new Thread(MethodWithParameter);
thread2.Start("Привет из потока!");
thread2.Join();
Console.WriteLine("2. Поток получил строковый параметр\n");

// -------------------------------------------------------------
// 3. Поток с лямбда-выражением (захват переменной)
// -------------------------------------------------------------
int value = 42;
Thread thread3 = new Thread(() =>
{
    Console.WriteLine($"   Лямбда-поток (ID {Thread.CurrentThread.ManagedThreadId}): значение = {value}");
});
thread3.Start();
thread3.Join();
Console.WriteLine("3. Лямбда захватывает переменную из внешнего контекста\n");

// -------------------------------------------------------------
// 4. Пользовательский (foreground) vs Фоновый (background)
// -------------------------------------------------------------
Console.WriteLine("4. Фоновый поток (background) завершается при закрытии основного потока");

Thread backgroundThread = new Thread(() =>
{
    for (int i = 0; i < 5; i++)
    {
        Console.WriteLine($"   Фоновый поток: шаг {i+1} (ID {Thread.CurrentThread.ManagedThreadId})");
        Thread.Sleep(300);
    }
});
backgroundThread.IsBackground = true; // <-- фоновый режим
backgroundThread.Start();

// Даём фоновому потоку немного поработать
Thread.Sleep(800);
Console.WriteLine("   Основной поток завершается. Фоновый поток будет принудительно остановлен.\n");
// Замечание: если бы поток был пользовательским (IsBackground = false), программа ждала бы его завершения.

// -------------------------------------------------------------
// 5. Thread.Join и ожидание завершения
// -------------------------------------------------------------
Thread worker = new Thread(() =>
{
    Thread.Sleep(500);
    Console.WriteLine("   Рабочий поток закончил работу");
});
worker.Start();
Console.WriteLine("5. Ожидание завершения потока через Join()");
worker.Join(); // блокируем текущий поток, пока worker не завершится
Console.WriteLine("   Join() вернул управление\n");

// -------------------------------------------------------------
// 6. Статические свойства потока: CurrentThread, ManagedThreadId, Name
// -------------------------------------------------------------
Thread current = Thread.CurrentThread;
current.Name = "MainThread";
Console.WriteLine("6. Свойства потока:");
Console.WriteLine($"   Имя: {current.Name ?? "<нет>"}");
Console.WriteLine($"   ManagedThreadId: {current.ManagedThreadId}");
Console.WriteLine($"   IsBackground: {current.IsBackground}");
Console.WriteLine($"   ThreadState: {current.ThreadState}\n");

// -------------------------------------------------------------
// 7. Передача нескольких параметров через кортеж или класс
// -------------------------------------------------------------
void MethodWithTuple(object state)
{
    var (x, y) = ((int, int))state;
    Console.WriteLine($"   Поток (ID {Thread.CurrentThread.ManagedThreadId}): x={x}, y={y}, sum={x+y}");
}

Thread thread4 = new Thread(MethodWithTuple);
thread4.Start((10, 20));
thread4.Join();
Console.WriteLine("7. Передача двух параметров через ValueTuple\n");

// -------------------------------------------------------------
// 8. Прерывание потока (Thread.Interrupt) – редко, но показательно
// -------------------------------------------------------------
Console.WriteLine("8. Прерывание спящего потока (Thread.Interrupt)");
Thread sleepingThread = new Thread(() =>
{
    try
    {
        Console.WriteLine("   Спящий поток засыпает на 5 секунд...");
        Thread.Sleep(5000);
        Console.WriteLine("   Спящий поток проснулся (не должен случиться)");
    }
    catch (ThreadInterruptedException)
    {
        Console.WriteLine("   Спящий поток был прерван!");
    }
});
sleepingThread.Start();
Thread.Sleep(500); // даём потоку уснуть
sleepingThread.Interrupt(); // прерываем ожидание
sleepingThread.Join();
Console.WriteLine();

// -------------------------------------------------------------
// 9. Отложенный запуск через параметр в Start(object)
// -------------------------------------------------------------
void DelayedStart(object delayMs)
{
    int delay = (int)delayMs;
    Thread.Sleep(delay);
    Console.WriteLine($"   Поток запущен с задержкой {delay} мс (ID {Thread.CurrentThread.ManagedThreadId})");
}
Thread delayedThread = new Thread(DelayedStart);
delayedThread.Start(1000);
delayedThread.Join();
Console.WriteLine("9. Поток стартовал с задержкой через передачу параметра\n");

