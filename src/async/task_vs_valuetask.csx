#nullable disable

using System;
using System.Threading.Tasks;


// - Task<T> – ссылочный тип, всегда создаёт объект в куче (аллокация).
// - ValueTask<T> – структурный тип, может избежать аллокации в некоторых сценариях.
// - Когда операция часто завершается синхронно (например, кэшированный результат), ValueTask<T> эффективнее.
// - Когда операция всегда асинхронна (например, сетевой запрос), Task<T> предпочтительнее,
//   т.к. ValueTask<T> требует осторожности (нельзя await дважды, нельзя использовать в конструкциях типа Task.WhenAll).
// - ValueTask<T> также можно использовать с IValueTaskSource для пулинга, но это сложный сценарий.


// Что демонстрируется
// Синхронное завершение: ValueTask без аллокации vs Task.FromResult с аллокацией.
// Асинхронное завершение: оба варианта работают, но ValueTask менее гибок.
// Ограничение ValueTask: нельзя await дважды (показываем, как обойти через .AsTask()).
// ValueTask не работает с Task.WhenAll и Task.WhenAny.
// Пример замера производительности (концептуально – без точного бенчмарка, но показывает разницу).



// 1. Пример: синхронное завершение – ValueTask выгоднее
// -------------------------------------------------------------
Console.WriteLine("1. Синхронное завершение (кэш)");

int cachedResult = 42;

// Task<int> – всегда аллокация
Task<int> GetTaskSync()
{
    return Task.FromResult(cachedResult); // создаёт Task<int> в куче
}

// ValueTask<int> – без аллокации (если результат известен синхронно)
ValueTask<int> GetValueTaskSync()
{
    return new ValueTask<int>(cachedResult); // структура на стеке
}

// Вызовы
Console.WriteLine($"   Task.FromResult: {await GetTaskSync()}");
Console.WriteLine($"   ValueTask (синхронно): {await GetValueTaskSync()}");
Console.WriteLine();


// 2. Пример: асинхронное завершение – Task предпочтительнее
Console.WriteLine("2. Асинхронное завершение (сетевой вызов)");

async Task<int> GetTaskAsync()
{
    await Task.Delay(10); // всегда асинхронный
    return 42;
}

// ValueTask для асинхронной операции – тоже можно, но осторожно
async ValueTask<int> GetValueTaskAsync()
{
    await Task.Delay(10);
    return 42;
}

Console.WriteLine($"   Task: {await GetTaskAsync()}");
Console.WriteLine($"   ValueTask: {await GetValueTaskAsync()}");
Console.WriteLine();


// 3. Ограничения ValueTask: нельзя await дважды
Console.WriteLine("3. Ограничения ValueTask:");

ValueTask<int> vt = GetValueTaskAsync();
int first = await vt;
// int second = await vt; // ОШИБКА! ValueTask нельзя использовать повторно.
Console.WriteLine($"   Можно await только один раз. Первое значение: {first}");
Console.WriteLine("   (Повторный await вызовет исключение)");
Console.WriteLine();


// 4. Как правильно обрабатывать ValueTask, если нужно несколько раз
Console.WriteLine("4. Если нужно многократное использование – используйте .AsTask()");

ValueTask<int> vt2 = GetValueTaskAsync();
Task<int> taskFromVt = vt2.AsTask(); // конвертируем в Task
int result1 = await taskFromVt;
int result2 = await taskFromVt; // теперь можно
Console.WriteLine($"   .AsTask() позволяет использовать несколько раз: {result1}, {result2}");
Console.WriteLine();


// 5. Когда ValueTask не подходит: Task.WhenAll, Task.WhenAny
Console.WriteLine("5. ValueTask нельзя использовать с Task.WhenAll/WhenAny");

async Task<int> AsyncOp(int id)
{
    await Task.Delay(id * 10);
    return id;
}

// Создаём массив Task<int>
Task<int>[] tasks = new[] { AsyncOp(1), AsyncOp(2), AsyncOp(3) };
int[] results = await Task.WhenAll(tasks);
Console.WriteLine($"   Task.WhenAll с Task: [{string.Join(", ", results)}]");

// Попытка использовать ValueTask в WhenAll не скомпилируется (нужно преобразовать в Task)
// ValueTask<int>[] vtTasks = new[] { GetValueTaskAsync(), GetValueTaskAsync() };
// await Task.WhenAll(vtTasks); // Ошибка компиляции
Console.WriteLine("   ValueTask нельзя напрямую передать в Task.WhenAll.");
Console.WriteLine();


// 6. Сравнение производительности (концептуальное)
Console.WriteLine("6. Производительность (концептуально):");

const int iterations = 1_000_000;

// Синхронный сценарий
long start = Environment.TickCount64;
for (int i = 0; i < iterations; i++)
{
    var t = Task.FromResult(i);
    int v = t.Result; // не используем await для чистоты замера
}
long taskTime = Environment.TickCount64 - start;

start = Environment.TickCount64;
for (int i = 0; i < iterations; i++)
{
    var vt = new ValueTask<int>(i);
    int v = vt.Result;
}
long vtTime = Environment.TickCount64 - start;

Console.WriteLine($"   Task.FromResult для {iterations} итераций: {taskTime} мс");
Console.WriteLine($"   ValueTask (синхронно) для {iterations} итераций: {vtTime} мс");
Console.WriteLine("   ValueTask значительно быстрее в синхронных сценариях (без аллокаций).");
Console.WriteLine();


// 7. Рекомендации по использованию
Console.WriteLine("7. Рекомендации:");
Console.WriteLine("   Используйте Task<T>, если операция почти всегда асинхронна (IO, сеть).");
Console.WriteLine("   Используйте ValueTask<T>, если операция часто синхронна (кэш, быстрые вычисления).");
Console.WriteLine("   Не используйте ValueTask, если вы планируете await несколько раз или использовать в WhenAll.");
Console.WriteLine("   Для пулов памяти и высоконагруженных сценариев используйте IValueTaskSource (см. документацию).");

