#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;


// - По умолчанию после await выполнение продолжается в том же контексте (SynchronizationContext),
//   который был активен до await (например, UI поток, ASP.NET контекст).
// - ConfigureAwait(false) указывает, что продолжение может выполняться в любом пуле потоков,
//   не возвращаясь в исходный контекст.
// - Это повышает производительность и предотвращает deadlock'и (особенно в UI/ASP.NET при синхронных вызовах).
// - Для библиотек рекомендуется использовать ConfigureAwait(false) везде, где это возможно.
// - Для UI-приложений (WPF/WinForms) важно знать, когда контекст нужен (доступ к UI элементам).

// Что демонстрируется
// Кастомный SynchronizationContext – логирует, когда вызывается Post (продолжение после await). Видно, что по умолчанию контекст захватывается, а с ConfigureAwait(false) – нет.
// Deadlock (концептуально) – показываем, как синхронный вызов .Result может привести к deadlock в UI/ASP.NET контексте, и как ConfigureAwait(false) помогает избежать этого (хотя всё равно синхронное ожидание не рекомендуется).
// Рекомендации – когда использовать ConfigureAwait(false), а когда нет.



// 1. Демонстрация контекста синхронизации (кастомный)
Console.WriteLine("1. Захват контекста (SynchronizationContext):");

// Создаём кастомный контекст, который логирует
class LoggingContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object state)
    {
        Console.WriteLine("   [LoggingContext] Post – продолжение поставлено в очередь");
        base.Post(d, state);
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        Console.WriteLine("   [LoggingContext] Send – синхронный вызов");
        base.Send(d, state);
    }
}

// Устанавливаем кастомный контекст для текущего потока
var originalContext = SynchronizationContext.Current;
SynchronizationContext.SetSynchronizationContext(new LoggingContext());
Console.WriteLine("   Установлен кастомный SynchronizationContext");

async Task TestContextAsync()
{
    Console.WriteLine($"   До await: контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
    await Task.Delay(1); // асинхронная пауза
    Console.WriteLine($"   После await: контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"} (должен быть LoggingContext)");
}

await TestContextAsync();

// Теперь с ConfigureAwait(false)
async Task TestConfigureAwaitFalseAsync()
{
    Console.WriteLine($"   До await: контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
    await Task.Delay(1).ConfigureAwait(false);
    Console.WriteLine($"   После await с ConfigureAwait(false): контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"} (не должен быть LoggingContext)");
}

await TestConfigureAwaitFalseAsync();

// Восстанавливаем исходный контекст (для чистоты)
SynchronizationContext.SetSynchronizationContext(originalContext);
Console.WriteLine();


// 2. Демонстрация deadlock при синхронном ожидании async метода без ConfigureAwait(false)
Console.WriteLine("2. Демонстрация deadlock (в UI/ASP.NET контексте):");

// Имитируем UI контекст – однопоточный, с захватом
class UISynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object state)
    {
        // В реальном UI контексте Post ставит в очередь сообщений, а мы просто выполняем синхронно для имитации блокировки
        Console.WriteLine("   [UI Context] Post – выполняется синхронно (имитация)");
        d(state);
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        Console.WriteLine("   [UI Context] Send – выполняется синхронно");
        d(state);
    }
}

// Устанавливаем UI контекст
SynchronizationContext.SetSynchronizationContext(new UISynchronizationContext());

async Task<int> GetValueAsync()
{
    await Task.Delay(100); // асинхронная операция
    return 42;
}

// Опасный метод: синхронный вызов .Result без ConfigureAwait(false)
void DangerousCall()
{
    Console.WriteLine("   Попытка синхронного вызова .Result (может вызвать deadlock)");
    try
    {
        int value = GetValueAsync().Result; // потенциальный deadlock
        Console.WriteLine($"   Результат: {value}");
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"   Исключение: {ex.InnerException?.Message}");
    }
}

// В скрипте нет реального deadlock, т.к. контекст не поддерживает захват, но покажем принцип
Console.WriteLine("   (В реальном UI/ASP.NET приложении синхронный вызов .Result с захватом контекста привёл бы к deadlock)");
// Для демонстрации просто вызовем безопасный вариант с ConfigureAwait(false)
async Task<int> GetValueWithConfigureAwaitAsync()
{
    await Task.Delay(100).ConfigureAwait(false);
    return 42;
}

void SafeCall()
{
    Console.WriteLine("   Безопасный вызов через .ConfigureAwait(false) + .Result");
    try
    {
        int value = GetValueWithConfigureAwaitAsync().Result; // тоже блокирует, но без deadlock (т.к. контекст не захвачен)
        Console.WriteLine($"   Результат: {value}");
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"   Исключение: {ex.InnerException?.Message}");
    }
}

// Вызываем
// DangerousCall(); // Раскомментировать, чтобы увидеть потенциальный deadlock (в скрипте может не произойти)
SafeCall();
Console.WriteLine();


// 3. Рекомендации и пояснения
Console.WriteLine("3. Рекомендации:");
Console.WriteLine("   В библиотеках всегда используйте .ConfigureAwait(false) после каждого await.");
Console.WriteLine("   В UI-приложениях (WPF/WinForms) используйте .ConfigureAwait(false) везде, где не нужен UI поток.");
Console.WriteLine("   Если нужен доступ к UI элементам, не используйте .ConfigureAwait(false) (чтобы сохранить контекст).");
Console.WriteLine("   В ASP.NET Core контекст обычно отсутствует, но ConfigureAwait(false) всё равно полезен для производительности.");
Console.WriteLine("   Избегайте синхронных вызовов .Result или .Wait() – используйте async/await по всей цепочке.");

// Восстанавливаем контекст
SynchronizationContext.SetSynchronizationContext(null);
