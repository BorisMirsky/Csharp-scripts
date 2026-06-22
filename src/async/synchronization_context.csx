#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;


// - SynchronizationContext – абстракция, представляющая среду выполнения (UI поток, ASP.NET контекст, пул потоков).
// - Захватывается перед await и восстанавливается после, если не использовать ConfigureAwait(false).
// - В UI-приложениях (WPF/WinForms) контекст гарантирует выполнение кода в UI потоке.
// - В ASP.NET (не Core) контекст обеспечивает доступ к HttpContext.Current и другим специфичным данным.
// - В консольных приложениях контекст по умолчанию равен null (используется пул потоков).
// - Можно создавать кастомные контексты для тестирования или специальных сценариев.

// Что демонстрируется
// Текущий контекст в консольном приложении (обычно null).
// Создание и установка кастомного контекста, логирующего поток выполнения.
// Разница между захватом контекста по умолчанию и с ConfigureAwait(false).
// Имитация UI-контекста (однопоточный) – показывает, что после await контекст восстанавливается (поток может измениться, но контекст тот же).
// Ручное восстановление контекста после использования ConfigureAwait(false).
// Рекомендации по использованию.



// 1. Текущий контекст по умолчанию (консольное приложение)
Console.WriteLine("1. Текущий SynchronizationContext:");
Console.WriteLine($"   SynchronizationContext.Current = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
Console.WriteLine("   (В консольных приложениях контекст обычно null, используется пул потоков)");
Console.WriteLine();


// 2. Кастомный контекст для отслеживания потока
Console.WriteLine("2. Создание кастомного контекста:");

class CustomSynchronizationContext : SynchronizationContext
{
    private readonly string _name;
    private readonly int _threadId;

    public CustomSynchronizationContext(string name)
    {
        _name = name;
        _threadId = Thread.CurrentThread.ManagedThreadId;
        Console.WriteLine($"   Создан контекст '{_name}' на потоке {_threadId}");
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        Console.WriteLine($"   [{_name}] Post: продолжение будет выполнено асинхронно (поток {Thread.CurrentThread.ManagedThreadId})");
        // Просто вызываем делегат на том же потоке (в реальном UI контексте это бы ставило в очередь)
        // Для имитации UI контекста мы можем выполнить на том же потоке, но чтобы показать разницу, выполним в пуле потоков.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Console.WriteLine($"   [{_name}] Post: выполнение на потоке {Thread.CurrentThread.ManagedThreadId}");
            d(state);
        });
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        Console.WriteLine($"   [{_name}] Send: синхронный вызов на потоке {Thread.CurrentThread.ManagedThreadId}");
        d(state);
    }
}

// Устанавливаем кастомный контекст
var originalContext = SynchronizationContext.Current;
var customContext = new CustomSynchronizationContext("MyContext");
SynchronizationContext.SetSynchronizationContext(customContext);

Console.WriteLine($"   Установлен контекст '{customContext.GetType().Name}'");
Console.WriteLine();


// 3. Демонстрация захвата контекста после await
Console.WriteLine("3. Захват контекста после await (по умолчанию):");

async Task TestDefaultAwaitAsync()
{
    Console.WriteLine($"   До await: поток {Thread.CurrentThread.ManagedThreadId}, контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
    await Task.Delay(10);
    Console.WriteLine($"   После await: поток {Thread.CurrentThread.ManagedThreadId}, контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
}

await TestDefaultAwaitAsync();
Console.WriteLine();


// 4. Демонстрация с ConfigureAwait(false) – контекст не захватывается
Console.WriteLine("4. Без захвата контекста (ConfigureAwait(false)):");

async Task TestConfigureAwaitFalseAsync()
{
    Console.WriteLine($"   До await: поток {Thread.CurrentThread.ManagedThreadId}, контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
    await Task.Delay(10).ConfigureAwait(false);
    Console.WriteLine($"   После await (ConfigureAwait(false)): поток {Thread.CurrentThread.ManagedThreadId}, контекст = {SynchronizationContext.Current?.GetType().Name ?? "null"}");
}

await TestConfigureAwaitFalseAsync();
Console.WriteLine();


// 5. Имитация UI-контекста (однопоточный контекст)
Console.WriteLine("5. Имитация UI-контекста (однопоточный):");

class UISynchronizationContext : SynchronizationContext
{
    private readonly int _uiThreadId;

    public UISynchronizationContext()
    {
        _uiThreadId = Thread.CurrentThread.ManagedThreadId;
        Console.WriteLine($"   UI контекст создан на потоке {_uiThreadId}");
    }

    public override void Post(SendOrPostCallback d, object state)
    {
        // В реальном UI контексте это добавляет сообщение в очередь, но здесь мы просто выполняем на том же потоке
        if (Thread.CurrentThread.ManagedThreadId == _uiThreadId)
        {
            Console.WriteLine($"   UI Post: выполняется немедленно на UI потоке {_uiThreadId}");
            d(state);
        }
        else
        {
            Console.WriteLine($"   UI Post: переключение на UI поток {_uiThreadId}");
            // В реальности здесь была бы логика отправки сообщения, но для демонстрации мы выполним синхронно
            // с предупреждением, что это имитация.
            d(state);
        }
    }

    public override void Send(SendOrPostCallback d, object state)
    {
        if (Thread.CurrentThread.ManagedThreadId == _uiThreadId)
        {
            d(state);
        }
        else
        {
            Console.WriteLine($"   UI Send: переключение на UI поток {_uiThreadId} (блокирующее)");
            // Имитация блокировки – выполняем на UI потоке (но в скрипте мы не можем реально переключиться)
            // Поэтому просто вызовем делегат на текущем потоке с предупреждением.
            Console.WriteLine("   (В реальном UI контексте здесь был бы переход на UI поток)");
            d(state);
        }
    }
}

// Устанавливаем UI контекст
SynchronizationContext.SetSynchronizationContext(new UISynchronizationContext());
Console.WriteLine("   Установлен UI контекст (имитация)");

async Task TestUIContextAsync()
{
    Console.WriteLine($"   До await (UI контекст): поток {Thread.CurrentThread.ManagedThreadId}");
    await Task.Delay(10);
    Console.WriteLine($"   После await (UI контекст): поток {Thread.CurrentThread.ManagedThreadId} (должен быть тот же, если контекст захвачен)");
}

await TestUIContextAsync();

// Теперь с ConfigureAwait(false)
async Task TestUIContextFalseAsync()
{
    Console.WriteLine($"   До await (UI контекст с ConfigureAwait(false)): поток {Thread.CurrentThread.ManagedThreadId}");
    await Task.Delay(10).ConfigureAwait(false);
    Console.WriteLine($"   После await (UI контекст, ConfigureAwait(false)): поток {Thread.CurrentThread.ManagedThreadId} (может отличаться)");
}

await TestUIContextFalseAsync();
Console.WriteLine();


// 6. Восстановление контекста вручную (если используем ConfigureAwait(false))
Console.WriteLine("6. Вручную восстановить контекст после ConfigureAwait(false):");

async Task ManualRestoreContextAsync()
{
    var capturedContext = SynchronizationContext.Current; // сохраняем до await
    Console.WriteLine($"   До await (с сохранением контекста): поток {Thread.CurrentThread.ManagedThreadId}");
    await Task.Delay(10).ConfigureAwait(false);
    Console.WriteLine($"   После await без контекста: поток {Thread.CurrentThread.ManagedThreadId}");
    // Восстанавливаем контекст вручную
    if (capturedContext != null)
    {
        capturedContext.Post(_ =>
        {
            Console.WriteLine($"   Вручную восстановленный контекст: поток {Thread.CurrentThread.ManagedThreadId}");
        }, null);
    }
    else
    {
        Console.WriteLine("   Контекст не был сохранён (null)");
    }
}

await ManualRestoreContextAsync();
Console.WriteLine();


// 7. Практические рекомендации
Console.WriteLine("7. Рекомендации:");
Console.WriteLine(" В библиотеках используйте ConfigureAwait(false), чтобы не зависеть от контекста.");
Console.WriteLine(" В UI-приложениях не используйте ConfigureAwait(false) в методах, обращающихся к UI элементам.");
Console.WriteLine(" В ASP.NET Core контекст обычно null, но ConfigureAwait(false) всё равно полезен для производительности.");
Console.WriteLine(" Для тестирования можно создавать кастомные контексты для симуляции различных сред.");

// Восстанавливаем исходный контекст (если был)
SynchronizationContext.SetSynchronizationContext(originalContext);

