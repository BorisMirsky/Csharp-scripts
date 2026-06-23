#nullable disable

using System;
using System.Diagnostics.Tracing;
using System.Threading;


// - EventListener – базовый класс для перехвата событий от поставщиков (EventSource).
// - .NET Runtime поставляет события через встроенные EventSource (например, Microsoft-Windows-DotNETRuntime).
// - События включают: GC, ThreadPool, JIT, исключения, аллокации.
// - Чтобы получать события, нужно подписаться на нужные ключевые слова (Keywords).
// - Для GC можно использовать Keywords: GCKeyword (0x1), GCHandleKeyword (0x2), GCHeapDumpKeyword (0x100000).
// - EventListener работает внутри процесса, не требует прав администратора (в отличие от PerfView).
// - Однако в .NET Core / .NET 5+ часть событий может быть недоступна без дополнительных флагов.



// 1. Создание кастомного EventListener
public class RuntimeEventListener : EventListener
{
    // Переопределяем метод, который вызывается при записи события
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Фильтруем только события GC (по имени или по ID)
        if (eventData.EventSource.Name == "Microsoft-Windows-DotNETRuntime" && 
            eventData.EventId >= 1 && eventData.EventId <= 20) // основные GC события
        {
            Console.WriteLine($"   [GC Event] EventId={eventData.EventId}, EventName={eventData.EventName}");
            // Выводим некоторые параметры (если есть)
            if (eventData.Payload != null && eventData.Payload.Count > 0)
            {
                for (int i = 0; i < Math.Min(eventData.Payload.Count, 3); i++)
                {
                    Console.WriteLine($"      Payload[{i}] = {eventData.Payload[i]}");
                }
            }
        }
        else if (eventData.EventSource.Name == "Microsoft-Windows-DotNETRuntime" &&
                 eventData.EventId == 31) // ThreadPool событие (пример)
        {
            Console.WriteLine($"   [ThreadPool Event] {eventData.EventName}");
        }
        // Можно добавить другие интересующие события
    }

    // Переопределяем метод, который вызывается при создании источника
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        Console.WriteLine($"   Источник событий создан: {eventSource.Name}");

        // Подписываемся на события Runtime с определёнными ключевыми словами
        if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
        {
            // Подписываемся на GC и ThreadPool события (ключевые слова)
            // GCKeyword = 0x1, ThreadPoolKeyword = 0x10000
            // Можно комбинировать: 0x1 | 0x10000 = 0x10001
            EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x10001);
            Console.WriteLine("   Подписка на GC и ThreadPool события активирована.");
        }
        // Можно подписаться и на другие источники, например, "System.Threading.Tasks.TplEventSource"
        else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
        {
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
            Console.WriteLine($"   Подписка на {eventSource.Name} активирована.");
        }
    }
}


// 2. Создание и запуск прослушивателя
Console.WriteLine("1. Создание EventListener...");
RuntimeEventListener listener = new RuntimeEventListener();

// Дадим слушателю время инициализироваться (он сам подпишется через OnEventSourceCreated)
Console.WriteLine("   Ожидание инициализации...");
Thread.Sleep(100);


// 3. Генерация событий: создаём объекты, вызываем GC
Console.WriteLine("\n2. Генерация событий (создание объектов и GC):");

// Создаём нагрузку, чтобы вызвать GC-события
for (int i = 0; i < 50; i++)
{
    byte[] arr = new byte[10000];
    Thread.Sleep(1); // небольшая задержка для разделения событий
}

Console.WriteLine("   Вызов GC.Collect...");
GC.Collect(0);
GC.WaitForPendingFinalizers();

Thread.Sleep(200); // даём время на обработку событий



// 4. Завершение и очистка
Console.WriteLine("\n3. Завершение (выключаем прослушивание)");
listener.Dispose();
Console.WriteLine("   EventListener отключён.");


// 5. Пояснение
Console.WriteLine("\nПримечание:");
Console.WriteLine("   Не все события могут отображаться в консольном приложении.");
Console.WriteLine("   Для получения более детальной информации используйте PerfView или dotnet-trace.");
Console.WriteLine("   В .NET Core/5+ события могут требовать настройки переменных окружения.");
Console.WriteLine("   Например: DOTNET_EnableEventLog=1");

