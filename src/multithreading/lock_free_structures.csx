#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


// - Lock_free алгоритмы не используют блокировки (lock, Monitor, Mutex).
// - Вместо этого применяются атомарные операции (Interlocked) и сравнение с обменом (CAS).
// - Преимущества: нет риска deadlock, высокая масштабируемость, предсказуемая задержка.
// - Недостатки: сложнее в реализации, могут вызывать livelock (бесконечные попытки).
// - Основные инструменты: Interlocked.Increment/Decrement/Add/Exchange/CompareExchange,
//   Volatile.Read/Write, Thread.MemoryBarrier, SpinWait.


// 1. Атомарный счётчик (Interlocked.Increment)
Console.WriteLine("1. Атомарный счётчик (без lock)");
int counter = 0;
Parallel.For(0, 1000, _ => Interlocked.Increment(ref counter));
Console.WriteLine($"   Результат: {counter} (ожидалось 1000)\n");


// 2. Lock‑free стек на основе односвязного списка
Console.WriteLine("2. Lock_free стек (Push/Pop)");
class LockFreeStack<T>
{
    private class Node { public T Value; public Node Next; }
    private Node _head;
    
    public void Push(T value)
    {
        Node newNode = new Node { Value = value };
        do
        {
            newNode.Next = _head;
        }
        while (Interlocked.CompareExchange(ref _head, newNode, newNode.Next) != newNode.Next);
    }
    
    public bool TryPop(out T result)
    {
        Node current;
        do
        {
            current = _head;
            if (current == null) { result = default; return false; }
        }
        while (Interlocked.CompareExchange(ref _head, current.Next, current) != current);
        result = current.Value;
        return true;
    }
}

var stack = new LockFreeStack<int>();
Parallel.For(0, 100, i => stack.Push(i));
int sum = 0;
int popped = 0;
while (stack.TryPop(out int val)) { sum += val; popped++; }
Console.WriteLine($"   Извлечено {popped} элементов, сумма = {sum}\n");


// 3. SpinWait – активное ожидание без блокировки потока
Console.WriteLine("3. SpinWait (ожидание флага без переключения контекста)");
bool flag = false;
int spinResult = 0;
Task waiter = Task.Run(() =>
{
    var spinner = new SpinWait();
    while (!flag)
        spinner.SpinOnce(); // крутится в цикле, иногда уступая процессор
    spinResult = 42;
});
Task setter = Task.Run(() =>
{
    Thread.Sleep(100);
    flag = true;
});
await Task.WhenAll(waiter, setter);
Console.WriteLine($"   SpinWait завершился, spinResult = {spinResult}\n");


// 4. Volatile – запрет оптимизации компилятора/процессора
Console.WriteLine("4. Volatile.Read/Write (барьер памяти)");
volatile bool stopFlag = false;
int counterVolatile = 0;
Task producer = Task.Run(() =>
{
    for (int i = 0; i < 100; i++)
    {
        counterVolatile++;
        Thread.Sleep(1);
    }
    Volatile.Write(ref stopFlag, true); // гарантированная запись
});
Task consumer = Task.Run(() =>
{
    while (!Volatile.Read(ref stopFlag))
    {
        // работа
    }
    Console.WriteLine($"   Consumer обнаружил остановку, counterVolatile = {counterVolatile}");
});
await producer;
await consumer;
Console.WriteLine();


// 5. Lock_free очередь (один писатель, один читатель), без CAS, только для демонстрации принципа
Console.WriteLine("5. Lock_free очередь (single producer, single consumer)");
class SingleProducerSingleConsumerQueue<T>
{
    private readonly T[] _buffer;
    private int _head; // читатель
    private int _tail; // писатель
    public SingleProducerSingleConsumerQueue(int capacity)
    {
        _buffer = new T[capacity];
    }
    public bool TryEnqueue(T item)
    {
        int tail = _tail;
        int next = (tail + 1) % _buffer.Length;
        if (next == _head) return false;
        _buffer[tail] = item;
        // Volatile.Write гарантирует, что запись индекса видна другому потоку
        Volatile.Write(ref _tail, next);
        return true;
    }
    public bool TryDequeue(out T item)
    {
        int head = _head;
        if (head == _tail) { item = default; return false; }
        item = _buffer[head];
        Volatile.Write(ref _head, (head + 1) % _buffer.Length);
        return true;
    }
}

var queue = new SingleProducerSingleConsumerQueue<int>(100);
Task producerTask = Task.Run(() =>
{
    for (int i = 0; i < 50; i++)
    {
        while (!queue.TryEnqueue(i)) Thread.Sleep(1);
    }
});
Task consumerTask = Task.Run(() =>
{
    int sumQueue = 0;
    int count = 0;
    while (count < 50)
    {
        if (queue.TryDequeue(out int val)) { sumQueue += val; count++; }
        else Thread.Sleep(1);
    }
    Console.WriteLine($"   Потребитель получил сумму = {sumQueue}");
});
await Task.WhenAll(producerTask, consumerTask);
Console.WriteLine();


// 6. Thread.MemoryBarrier (предотвращение переупорядочивания)
Console.WriteLine("6. MemoryBarrier (демонстрация без переупорядочивания)");
bool a = false, b = false;
int x = 0, y = 0;
Task t1 = Task.Run(() =>
{
    x = 1;
    Thread.MemoryBarrier(); // гарантия, что запись x завершится до записи a
    a = true;
});
Task t2 = Task.Run(() =>
{
    y = 2;
    Thread.MemoryBarrier();
    b = true;
});
await Task.WhenAll(t1, t2);
Console.WriteLine($"   a={a}, b={b}, x={x}, y={y} (обычно без барьеров могли бы увидеть x=0, y=0 при a=true, b=true)");
Console.WriteLine();
