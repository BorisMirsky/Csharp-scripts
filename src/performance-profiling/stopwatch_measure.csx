#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;


// - Stopwatch – высокоточный таймер для измерения времени выполнения кода.
// - Для получения достоверных результатов:
//   1) Прогревочный прогон – игнорируем первый запуск (JIT компиляция).
//   2) Несколько итераций – усредняем, отбрасываем выбросы.
//   3) Учитываем накладные расходы (измеряем пустой вызов).
// - Stopwatch измеряет время в тактах (ElapsedTicks) или в миллисекундах (ElapsedMilliseconds).
// - Для высокоточной статистики используем BenchmarkDotNet (следующий файл).

// Что демонстрируется
// Простой замер времени.
// Замер с прогревочным прогоном (игнорируем JIT) и усреднением.
// Сравнение двух методов (быстрого и медленного).
// Замер метода с параметрами через замыкание.
// Оценка накладных расходов Stopwatch.
// Информация о частоте и разрешении таймера.
// Замер асинхронного метода (Task.Delay).


// 1. Простейший замер
Console.WriteLine("1. Простейший замер:");

void SimpleMethod()
{
    Thread.Sleep(100); // имитация работы
}

Stopwatch sw = Stopwatch.StartNew();
SimpleMethod();
sw.Stop();
Console.WriteLine($"   Время выполнения SimpleMethod: {sw.ElapsedMilliseconds} мс");
Console.WriteLine();


// 2. Замер с прогревочным прогоном и усреднением
Console.WriteLine("2. Замер с прогревочным прогоном (игнорируем JIT):");

// Метод для замера
void WorkMethod(int iterations)
{
    for (int i = 0; i < iterations; i++)
    {
        // имитация вычислений
        Math.Sqrt(i);
    }
}

// Функция замера с прогревочным прогоном и усреднением
double MeasureTime(Action action, int warmupCount = 1, int measureCount = 5)
{
    // Прогревочный прогон (JIT + оптимизации)
    for (int i = 0; i < warmupCount; i++)
        action();

    // Измеряем measureCount раз и усредняем
    long[] ticks = new long[measureCount];
    for (int i = 0; i < measureCount; i++)
    {
        GC.Collect(); // стараемся минимизировать влияние GC
        GC.WaitForPendingFinalizers();
        long start = Stopwatch.GetTimestamp();
        action();
        long end = Stopwatch.GetTimestamp();
        ticks[i] = end - start;
    }

    // Переводим в миллисекунды (среднее)
    double avgTicks = ticks.Average();
    double ms = (avgTicks / Stopwatch.Frequency) * 1000.0;
    return ms;
}

double timeMs = MeasureTime(() => WorkMethod(100_000), warmupCount: 2, measureCount: 5);
Console.WriteLine($"   Среднее время выполнения WorkMethod(100_000): {timeMs:F2} мс");
Console.WriteLine();


// 3. Сравнение двух методов с выводом статистики
Console.WriteLine("3. Сравнение двух методов:");

void FastMethod()
{
    int sum = 0;
    for (int i = 0; i < 100_000; i++)
        sum += i;
}

void SlowMethod()
{
    double sum = 0;
    for (int i = 0; i < 100_000; i++)
        sum += Math.Sqrt(i);
}

double fastTime = MeasureTime(FastMethod, 1, 5);
double slowTime = MeasureTime(SlowMethod, 1, 5);

Console.WriteLine($"   FastMethod: {fastTime:F2} мс");
Console.WriteLine($"   SlowMethod: {slowTime:F2} мс");
Console.WriteLine($"   SlowMethod в {slowTime / fastTime:F1} раз медленнее");
Console.WriteLine();


// 4. Измерение с передачей параметров через замыкание
Console.WriteLine("4. Замер метода с параметрами:");

void ParameterizedMethod(int n)
{
    double sum = 0;
    for (int i = 0; i < n; i++)
        sum += Math.Sin(i);
}

// Замыкание для передачи параметра
int size = 1_000_000;
double paramTime = MeasureTime(() => ParameterizedMethod(size), 1, 3);
Console.WriteLine($"   Время ParameterizedMethod({size}): {paramTime:F2} мс");
Console.WriteLine();


// 5. Оценка накладных расходов Stopwatch
Console.WriteLine("5. Измерение накладных расходов Stopwatch:");

double overhead = MeasureTime(() => { }, warmupCount: 0, measureCount: 10);
Console.WriteLine($"   Накладные расходы на вызов MeasureTime (пустой метод): {overhead:F4} мс");
Console.WriteLine("   (Это время на сам Stopwatch + вызов делегата)");
Console.WriteLine();


// 6. Замер с использованием ElapsedTicks и Frequency
Console.WriteLine("6. Подробности Stopwatch:");
Console.WriteLine($"   Stopwatch.Frequency = {Stopwatch.Frequency} (тактов в секунду)");
Console.WriteLine($"   Stopwatch.IsHighResolution = {Stopwatch.IsHighResolution}");
// Если IsHighResolution = true, используется аппаратный таймер.
Console.WriteLine();


// 7. Измерение асинхронного кода (с Task.Delay)
Console.WriteLine("7. Измерение асинхронного метода:");

async Task AsyncWorkAsync(int delayMs)
{
    await Task.Delay(delayMs);
}

async Task<double> MeasureAsyncTimeAsync(Func<Task> asyncAction, int warmupCount = 1, int measureCount = 3)
{
    for (int i = 0; i < warmupCount; i++)
        await asyncAction();

    long[] ticks = new long[measureCount];
    for (int i = 0; i < measureCount; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        long start = Stopwatch.GetTimestamp();
        await asyncAction();
        long end = Stopwatch.GetTimestamp();
        ticks[i] = end - start;
    }

    double avgTicks = ticks.Average();
    return (avgTicks / Stopwatch.Frequency) * 1000.0;
}

double asyncTime = await MeasureAsyncTimeAsync(() => AsyncWorkAsync(50), warmupCount: 1, measureCount: 3);
Console.WriteLine($"   AsyncWorkAsync(50) среднее время: {asyncTime:F2} мс (должно быть ~50 мс)");
Console.WriteLine();

