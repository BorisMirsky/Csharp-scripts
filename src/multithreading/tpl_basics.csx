#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


// 1. Простейший Task
Console.WriteLine("1. Task без результата:");
Task task1 = Task.Run(() =>
{
    Console.WriteLine($"   Задача выполняется в потоке ID {Thread.CurrentThread.ManagedThreadId}");
    Thread.Sleep(200);
});
await task1;
Console.WriteLine("   Завершён\n");

// 2. Task с возвратом значения
Console.WriteLine("2. Task с результатом:");
Task<int> task2 = Task.Run(() =>
{
    Console.WriteLine($"   Вычисление в потоке {Thread.CurrentThread.ManagedThreadId}");
    int sum = 0;
    for (int i = 1; i <= 10; i++) sum += i;
    return sum;
});
int result = await task2;
Console.WriteLine($"   Сумма 1..10 = {result}\n");

// 3. Task с async/await внутри
Console.WriteLine("3. Task с async/await:");
async Task<int> ComputeAsync()
{
    await Task.Delay(100);
    return 42;
}
int asyncResult = await ComputeAsync();
Console.WriteLine($"   Результат async-метода: {asyncResult}\n");

// 4. WhenAll / WhenAny
Console.WriteLine("4. Ожидание нескольких задач (WhenAll):");
Task<int> t1 = Task.Run(() => { Thread.Sleep(50); return 1; });
Task<int> t2 = Task.Run(() => { Thread.Sleep(30); return 2; });
int[] results = await Task.WhenAll(t1, t2);
Console.WriteLine($"   Результаты: {string.Join(", ", results)}");

Console.WriteLine("   WhenAny – завершившаяся первая задача:");
Task<int> fast = Task.Run(() => { Thread.Sleep(20); return 100; });
Task<int> slow = Task.Run(() => { Thread.Sleep(80); return 200; });
Task<int> completed = await Task.WhenAny(fast, slow);
Console.WriteLine($"   Первая завершённая задача вернула: {await completed}\n");

// 5. Обработка ошибок
Console.WriteLine("5. Обработка исключений:");
try
{
    Task bad = Task.Run(() =>
    {
        throw new InvalidOperationException("Ошибка внутри Task");
    });
    await bad;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   Перехвачено: {ex.Message}");
}
Console.WriteLine();

// 6. Отмена Task через CancellationToken (исправленный блок using)
Console.WriteLine("6. Отмена операции:");
using (var cts = new CancellationTokenSource())
{
    CancellationToken token = cts.Token;
    Task cancellableTask = Task.Run(() =>
    {
        for (int i = 0; i < 10; i++)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine($"   Шаг {i+1}");
            Thread.Sleep(100);
        }
    }, token);
    cts.CancelAfter(300);
    try
    {
        await cancellableTask;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("   Задача была отменена");
    }
}
Console.WriteLine();

// 7. Parallel.For
Console.WriteLine("7. Parallel.For:");
Parallel.For(0, 5, i =>
{
    int tid = Thread.CurrentThread.ManagedThreadId;
    Console.WriteLine($"   Итерация {i} на потоке {tid}");
});
Console.WriteLine();

// 8. Parallel.ForEach с накоплением
Console.WriteLine("8. Parallel.ForEach с накоплением:");
var data = Enumerable.Range(1, 100).ToList();
int sumTotal = 0;
object locker = new object();

Parallel.ForEach(data, () => 0, (item, state, localSum) =>
{
    localSum += item;
    return localSum;
},
localSum =>
{
    lock (locker) sumTotal += localSum;
});

Console.WriteLine($"   Сумма 1..100 = {sumTotal} (должно быть 5050)\n");

// 9. PLINQ
Console.WriteLine("9. PLINQ (AsParallel):");
var numbers = Enumerable.Range(1, 20);
var squares = numbers.AsParallel()
                     .Where(x => x % 2 == 0)
                     .Select(x => x * x)
                     .ToArray();
Console.WriteLine($"   Квадраты чётных чисел 1..20: {string.Join(", ", squares)}\n");

// 10. TaskCompletionSource
Console.WriteLine("10. TaskCompletionSource (ручное завершение):");
var tcs = new TaskCompletionSource<int>();
_ = Task.Run(async () =>
{
    await Task.Delay(200);
    tcs.SetResult(999);
});
int manualResult = await tcs.Task;
Console.WriteLine($"   Результат от TaskCompletionSource: {manualResult}\n");

