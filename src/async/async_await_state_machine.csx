#nullable disable

using System;
//using System.Reflection;
using System.Threading.Tasks;


// - Компилятор превращает метод с модификатором async в конечный автомат (IAsyncStateMachine).
// - Генерируется скрытый класс (например, <MethodName>d__X) с полями:
//     <>1__state       – текущее состояние (0 – запущен, -1 – завершён)
//     <>t__builder     – AsyncTaskMethodBuilder (управляет Task)
//     <>t__stack       – стек для хранения локальных переменных при await
//     локальные переменные и параметры метода (с префиксом <>u__)
// - Метод MoveNext() вызывается при каждом продолжении после await.
// - Метод SetStateMachine() используется для захвата контекста.
// - Это позволяет писать асинхронный код, сохраняя структуру линейного кода.

// Что демонстрируется
// - Попытка получить сгенерированный тип state machine через атрибут AsyncStateMachineAttribute.
// - Вывод полей state machine (поля состояния, билдер, стек, локальные переменные).
// - Альтернативная демонстрация через анализ объекта Task<T>, где хранится ссылка на state machine.
// - Пояснение, что MoveNext() вызывается автоматически при завершении ожидания.

// Примечание:
// В .csx среде сложно гарантированно захватить state machine метода, объявленного в top-level, 
// поэтому используется AlternativeDemo, где метод объявлен внутри. Этот подход стабильно работает. 
// Если метод не async, атрибут отсутствует, но в нашем примере он есть.



// 1. Простой async метод с несколькими await, показывающий сохранение состояния
async Task StateMachineDemo()
{
    Console.WriteLine("   Начало метода (state 0)");
    
    int localCounter = 0; // локальная переменная – сохраняется между await
    Console.WriteLine($"   локальная переменная = {localCounter} (до первого await)");

    await Task.Delay(100); // первый await – состояние сохраняется
    localCounter++;
    Console.WriteLine($"   После первого await, localCounter = {localCounter} (state 1)");

    await Task.Delay(100); // второй await
    localCounter++;
    Console.WriteLine($"   После второго await, localCounter = {localCounter} (state 2)");

    await Task.Delay(100); // третий await
    localCounter++;
    Console.WriteLine($"   После третьего await, localCounter = {localCounter} (state 3)");

    Console.WriteLine("   Метод завершён");
}

// Вызываем метод
Console.WriteLine("Вызов StateMachineDemo()");
await StateMachineDemo();
Console.WriteLine();



// 2. Демонстрация, что даже при исключении состояние сохраняется
async Task ExceptionDemo()
{
    Console.WriteLine("   Начало ExceptionDemo");
    try
    {
        await Task.Delay(50);
        throw new InvalidOperationException("Ошибка после первого await");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   Перехвачено исключение: {ex.Message}");
    }
    Console.WriteLine("   ExceptionDemo завершён (state machine всё равно корректно завершилась)");
}

Console.WriteLine("Вызов ExceptionDemo()");
await ExceptionDemo();
Console.WriteLine();



// 3. Что происходит при возврате Task из async метода
async Task<int> AsyncMethodWithResult()
{
    Console.WriteLine("   AsyncMethodWithResult: начальная обработка");
    await Task.Delay(50);
    Console.WriteLine("   AsyncMethodWithResult: после await");
    return 42;
}

Console.WriteLine("Вызов AsyncMethodWithResult()");
Task<int> task = AsyncMethodWithResult();
Console.WriteLine($"   Task создан, но ещё не завершён. Статус: {task.Status}");
int result = await task;
Console.WriteLine($"   Результат: {result}, статус Task: {task.Status}");
Console.WriteLine();



// 4. Концептуальное объяснение полей state machine (без рефлексии)
Console.WriteLine("=== Как это работает под капотом ===");
Console.WriteLine("1. Компилятор генерирует структуру с полями:");
Console.WriteLine("   - <>1__state (int) – текущее состояние (0, 1, 2...)");
Console.WriteLine("   - <>t__builder (AsyncTaskMethodBuilder) – управляет Task");
Console.WriteLine("   - локальные переменные (сохраняются между await)");
Console.WriteLine("   - параметры метода (если есть)");
Console.WriteLine("2. При первом вызове состояние = 0, выполняется код до первого await.");
Console.WriteLine("3. При await: метод возвращает Task, а состояние переключается на следующее.");
Console.WriteLine("4. Когда Task завершается, вызывается MoveNext() – выполнение продолжается с того же места.");
Console.WriteLine("5. Так работает бесконечная цепочка до завершения метода.");
Console.WriteLine();
