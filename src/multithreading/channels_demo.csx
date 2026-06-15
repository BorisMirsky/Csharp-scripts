#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;


// - Каналы (Channel<T>) – потокобезопасная очередь для передачи данных между producers и consumers.
// - Channel.CreateUnbounded<T>() – без ограничения размера.
// - Channel.CreateBounded<T>(capacity) – с фиксированной ёмкостью (поддержка ожидания, когда полон/пуст).
// - Writer.TryWrite / Writer.WriteAsync – отправка данных.
// - Reader.TryRead / Reader.ReadAsync / Reader.WaitToReadAsync – получение данных.
// - ChannelWriter.Complete() + Reader.Completion – сигнализация о завершении записи.
// - Поддерживает отмену через CancellationToken.
// - Идеален для высоконагруженных систем (например, обработка очередей сообщений).


// 1. Базовый канал: один producer, один consumer (Unbounded)
Console.WriteLine("1. Базовый канал (Unbounded)");

Channel<int> channel = Channel.CreateUnbounded<int>();
ChannelWriter<int> writer = channel.Writer;
ChannelReader<int> reader = channel.Reader;

Task producer1 = Task.Run(async () =>
{
    for (int i = 0; i < 5; i++)
    {
        await writer.WriteAsync(i);
        Console.WriteLine($"   Producer: отправлено {i}");
        await Task.Delay(100); // имитация работы
    }
    writer.Complete(); // сигнал, что запись завершена
});

Task consumer1 = Task.Run(async () =>
{
    await foreach (int item in reader.ReadAllAsync())
    {
        Console.WriteLine($"   Consumer: получено {item}");
    }
    Console.WriteLine("   Consumer: канал завершён (no more items)");
});

await Task.WhenAll(producer1, consumer1);
Console.WriteLine();


// 2. Ограниченный канал (Bounded) с блокировкой при переполнении
Console.WriteLine("2. Канал с ограничением ёмкости (Bounded, capacity=2)");

var boundedChannel = Channel.CreateBounded<int>(2);
var boundedWriter = boundedChannel.Writer;
var boundedReader = boundedChannel.Reader;

Task producer2 = Task.Run(async () =>
{
    for (int i = 0; i < 4; i++)
    {
        await boundedWriter.WriteAsync(i);
        Console.WriteLine($"   Producer2: отправлено {i}");
    }
    boundedWriter.Complete();
});

Task consumer2 = Task.Run(async () =>
{
    await foreach (int item in boundedReader.ReadAllAsync())
    {
        Console.WriteLine($"   Consumer2: получено {item}");
        await Task.Delay(200); // медленное потребление – покажем блокировку
    }
});

await Task.WhenAll(producer2, consumer2);
Console.WriteLine();


// 3. Множественные producers и consumers (concurrent)
Console.WriteLine("3. Несколько producers и consumers");

var multiChannel = Channel.CreateUnbounded<string>();
var multiWriter = multiChannel.Writer;
var multiReader = multiChannel.Reader;

Task[] producers = new Task[3];
for (int p = 0; p < 3; p++)
{
    int producerId = p;
    producers[p] = Task.Run(async () =>
    {
        for (int i = 0; i < 3; i++)
        {
            string msg = $"P{producerId}-{i}";
            await multiWriter.WriteAsync(msg);
            Console.WriteLine($"   Producer {producerId}: {msg}");
            await Task.Delay(50);
        }
    });
}

Task consumerMulti = Task.Run(async () =>
{
    int received = 0;
    await foreach (string item in multiReader.ReadAllAsync())
    {
        Console.WriteLine($"   Consumer: {item}");
        received++;
    }
    Console.WriteLine($"   Всего получено: {received}");
});

// Запускаем producers и закрываем writer после их завершения
await Task.WhenAll(producers);
multiWriter.Complete();
await consumerMulti;
Console.WriteLine();


// 4. Использование TryWrite / TryRead (неблокирующие операции)
Console.WriteLine("4. Неблокирующие операции TryWrite / TryRead");

var tryChannel = Channel.CreateBounded<int>(1);
var tryWriter = tryChannel.Writer;
var tryReader = tryChannel.Reader;

Console.WriteLine($"   Попытка записи 42: {tryWriter.TryWrite(42)}");
Console.WriteLine($"   Попытка записи 100 (канал полон): {tryWriter.TryWrite(100)}");
if (tryReader.TryRead(out int value))
    Console.WriteLine($"   Прочитано: {value}");
else
    Console.WriteLine("   Ничего не прочитано");
Console.WriteLine();


// 5. Отмена операции через CancellationToken
Console.WriteLine("5. Отмена ReadAsync через CancellationToken");

using (var cts = new CancellationTokenSource())
{
    var cancelChannel = Channel.CreateUnbounded<int>();
    var cancelWriter = cancelChannel.Writer;
    var cancelReader = cancelChannel.Reader;

    Task producerCancel = Task.Run(async () =>
    {
        for (int i = 0; i < 5; i++)
        {
            await cancelWriter.WriteAsync(i);
            Console.WriteLine($"   ProducerCancel: {i}");
            await Task.Delay(100);
        }
        cancelWriter.Complete();
    });

    Task consumerCancel = Task.Run(async () =>
    {
        try
        {
            await foreach (var item in cancelReader.ReadAllAsync(cts.Token))
            {
                Console.WriteLine($"   ConsumerCancel: {item}");
                if (item == 2)
                    cts.Cancel(); // отмена после получения 2
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   ConsumerCancel: операция отменена");
        }
    });

    await Task.WhenAll(producerCancel, consumerCancel);
}
Console.WriteLine();


// 6. Ожидание данных без выделения Task (WaitToReadAsync / WaitToWriteAsync)
Console.WriteLine("6. WaitToReadAsync / WaitToWriteAsync (эффективное ожидание)");

var waitChannel = Channel.CreateBounded<int>(1);
var waitWriter = waitChannel.Writer;
var waitReader = waitChannel.Reader;

Task producerWait = Task.Run(async () =>
{
    for (int i = 0; i < 3; i++)
    {
        await waitWriter.WaitToWriteAsync(); // ждём, когда появится место (для bounded)
        waitWriter.TryWrite(i);
        Console.WriteLine($"   ProducerWait: записан {i}");
        await Task.Delay(200);
    }
    waitWriter.Complete();
});

Task consumerWait = Task.Run(async () =>
{
    while (await waitReader.WaitToReadAsync())
    {
        while (waitReader.TryRead(out int item))
        {
            Console.WriteLine($"   ConsumerWait: прочитан {item}");
            await Task.Delay(400); // медленно, чтобы срабатывало WaitToWriteAsync
        }
    }
});

await Task.WhenAll(producerWait, consumerWait);
Console.WriteLine();

