#nullable disable

// Подключаем BenchmarkDotNet из NuGet
#r "nuget: BenchmarkDotNet, 0.13.12"

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System;
using System.Linq;

// 
// BENCHMARKDOTNET – профессиональный бенчмаркинг
// - BenchmarkDotNet – библиотека для точного и надёжного измерения производительности .NET кода.
// - Автоматически выполняет прогревочные прогоны, усреднение, отбрасывает выбросы.
// - Поддерживает различные рантаймы (.NET Framework, .NET Core, .NET 5+).
// - Можно сравнивать разные версии кода, параметры, инжектировать зависимости.
// - Результаты выводятся в консоль, а также могут быть экспортированы в HTML, CSV, Markdown.
// - Позволяет измерять время, аллокации, GC и другие метрики.
// - Для использования достаточно создать класс с атрибутами [Benchmark] и запустить BenchmarkRunner.Run<T>().



// 1. Определяем бенчмарк-класс
[MemoryDiagnoser] // добавим измерение аллокаций (может не работать в .csx, но для демонстрации оставим)
[ShortRunJob] // сокращаем время выполнения (меньше итераций)
public class StringConcatBenchmark
{
    private const int N = 1000;

    // Метод с обычной конкатенацией через +=
    [Benchmark(Baseline = true)]
    public string ConcatWithPlus()
    {
        string result = "";
        for (int i = 0; i < N; i++)
            result += i.ToString();
        return result;
    }

    // Метод с StringBuilder
    [Benchmark]
    public string ConcatWithStringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < N; i++)
            sb.Append(i);
        return sb.ToString();
    }

    // Метод с явным массивом и string.Concat
    [Benchmark]
    public string ConcatWithJoin()
    {
        var parts = new string[N];
        for (int i = 0; i < N; i++)
            parts[i] = i.ToString();
        return string.Concat(parts);
    }
}


// 2. Запуск бенчмарка
Console.WriteLine("Запуск бенчмарка (может занять некоторое время)...");
Console.WriteLine("(Будут выполнены прогревочные прогоны и измерения)");
Console.WriteLine();

// Создаём конфиг с минимальными настройками
var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator) // убираем предупреждения о релизной сборке
    .WithArtifactsPath(Environment.CurrentDirectory); // сохраняем результаты в текущую папку

// Запускаем бенчмарк
var summary = BenchmarkRunner.Run<StringConcatBenchmark>(config);


// 3. Вывод короткого резюме
Console.WriteLine($"Всего бенчмарков: {summary.BenchmarksCases.Count()}");

// Выводим только название и среднее время для каждого метода
Console.WriteLine("\nРезультаты (время в миллисекундах):");
foreach (var report in summary.Reports)
{
    var method = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
    var mean = report.ResultStatistics?.Mean ?? 0;
    Console.WriteLine($"   {method,-25} : среднее = {mean,10:F3} мс");
}

Console.WriteLine("\n(Измерение аллокаций в .csx может быть недоступно, но в отдельном проекте работает)");

