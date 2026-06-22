#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;


// - CancellationToken – структура, передаваемая в асинхронные методы для сигнала отмены.
// - CancellationTokenSource – управляет токеном: вызывает Cancel() или CancelAfter().
// - Методы должны периодически проверять token.IsCancellationRequested или вызывать token.ThrowIfCancellationRequested().
// - Отмена не является мгновенной – код должен кооперативно реагировать.
// - Таймауты реализуются через CancelAfter(TimeSpan) или через Task.WhenAny с таймером.
// - Важно: после отмены токен нельзя переиспользовать (создайте новый CancellationTokenSource).

// Что демонстрируется
// Базовая отмена через Cancel() и ThrowIfCancellationRequested().
// Таймаут через конструктор CancellationTokenSource(TimeSpan).
// Ручной таймаут через Task.WhenAny с Task.Delay – более гибкий, позволяет обработать таймаут и продолжить.
// Ручная проверка IsCancellationRequested без выбрасывания исключения.
// Отмена нескольких задач через общий токен.
// Регистрация callback через Register.


// 1. Базовая отмена через CancellationTokenSource.Cancel()
Console.WriteLine("1. Базовая отмена:");

async Task<int> LongRunningOperationAsync(CancellationToken token)
{
    int sum = 0;
    for (int i = 0; i < 100; i++)
    {
        token.ThrowIfCancellationRequested(); // выбрасывает OperationCanceledException
        sum += i;
        await Task.Delay(10, token); // передаём токен в Delay – он тоже может быть отменён
    }
    return sum;
}

using (var cts = new CancellationTokenSource())
{
    Task<int> task = LongRunningOperationAsync(cts.Token);
    
    Console.WriteLine("   Запущена долгая операция. Отмена через 200 мс...");
    cts.CancelAfter(200);
    
    try
    {
        int result = await task;
        Console.WriteLine($"   Результат: {result} (не должно выполниться)");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("   Операция отменена (OperationCanceledException)");
    }
}
Console.WriteLine();


// 2. Таймаут через CancelAfter (простой способ)
Console.WriteLine("2. Таймаут через CancelAfter:");

using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300))) // таймаут в конструкторе
{
    Console.WriteLine("   Запуск операции с таймаутом 300 мс (CancelAfter автоматически)");
    try
    {
        await LongRunningOperationAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("   Операция отменена по таймауту (автоматический CancelAfter)");
    }
}
Console.WriteLine();


// 3. Таймаут через Task.WhenAny (ручное управление, гибче)
Console.WriteLine("3. Таймаут через Task.WhenAny:");

async Task<int> OperationWithManualTimeoutAsync(int timeoutMs)
{
    using (var cts = new CancellationTokenSource())
    {
        var task = LongRunningOperationAsync(cts.Token);
        var timeoutTask = Task.Delay(timeoutMs);
        
        var completedTask = await Task.WhenAny(task, timeoutTask);
        if (completedTask == timeoutTask)
        {
            // Таймаут наступил, отменяем операцию
            cts.Cancel();
            Console.WriteLine("   Таймаут (WhenAny), операция отменена");
            // Ждём завершения задачи (она должна выбросить исключение)
            try { await task; }
            catch (OperationCanceledException) { /* ожидаемо */ }
            return -1;
        }
        else
        {
            // Операция завершилась успешно
            return await task;
        }
    }
}

int resultWithTimeout = await OperationWithManualTimeoutAsync(150);
Console.WriteLine($"   Результат с таймаутом 150 мс: {resultWithTimeout} (-1 означает таймаут)");

int resultWithTimeoutOk = await OperationWithManualTimeoutAsync(2000);
Console.WriteLine($"   Результат с таймаутом 2000 мс: {resultWithTimeoutOk} (успех)");
Console.WriteLine();




// 4. Проверка отмены в цикле (без ThrowIfCancellationRequested, без исключения)
Console.WriteLine("4. Проверка IsCancellationRequested (ручная обработка без исключения):");

async Task ManualCancellationCheckAsync(CancellationToken token)
{
    for (int i = 0; i < 20; i++)
    {
        // Ручная проверка – если отменено, выходим без исключения
        if (token.IsCancellationRequested)
        {
            Console.WriteLine($"   Отмена обнаружена на итерации {i}, выходим (без исключения)");
            return;
        }
        Console.WriteLine($"   Итерация {i}");
        // Используем Task.Delay без токена, чтобы он не выбрасывал исключение при отмене
        await Task.Delay(50);
    }
    Console.WriteLine("   Операция завершена нормально");
}

using (var cts = new CancellationTokenSource())
{
    cts.CancelAfter(200);
    await ManualCancellationCheckAsync(cts.Token);
}
Console.WriteLine();



// 5. Отмена нескольких операций (общий токен)
Console.WriteLine("5. Отмена нескольких задач через общий токен:");

using (var cts = new CancellationTokenSource())
{
    var token = cts.Token;
    var tasks = new Task<int>[]
    {
        LongRunningOperationAsync(token),
        LongRunningOperationAsync(token),
        LongRunningOperationAsync(token)
    };

    Console.WriteLine("   Запущено 3 задачи, отмена через 300 мс");
    cts.CancelAfter(300);

    try
    {
        await Task.WhenAll(tasks);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("   Одна или несколько задач отменены (общий токен)");
    }

    // Проверим статус каждой задачи
    foreach (var t in tasks)
    {
        Console.WriteLine($"   Статус задачи: {t.Status}");
    }
}
Console.WriteLine();


// 6. Отмена с использованием CancellationToken.Register (обратный вызов)
Console.WriteLine("6. Регистрация обратного вызова на отмену:");

using (var cts = new CancellationTokenSource())
{
    cts.Token.Register(() => Console.WriteLine("   [Callback] Отмена запрошена!"));
    Console.WriteLine("   Зарегистрирован callback, отмена через 100 мс");
    cts.CancelAfter(100);
    await Task.Delay(200); // даём время на выполнение callback
}
Console.WriteLine();


// 7. Рекомендации
Console.WriteLine("7. Рекомендации:");
Console.WriteLine("   Всегда передавайте CancellationToken в асинхронные методы, если они поддерживают отмену.");
Console.WriteLine("   Используйте ThrowIfCancellationRequested() для простых сценариев.");
Console.WriteLine("   Для длительных операций с циклами проверяйте IsCancellationRequested вручную.");
Console.WriteLine("   Таймауты реализуйте через CancelAfter или Task.WhenAny (выбор зависит от гибкости).");
Console.WriteLine("   Не используйте один CancellationTokenSource повторно после отмены – создавайте новый.");
Console.WriteLine("   CancellationToken.Register используйте с осторожностью (может вызвать утечки памяти, если не отписываться).");

