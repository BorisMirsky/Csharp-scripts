# Производительность и профилирование
Примеры кода и краткие руководства по измерению производительности и диагностике .NET-приложений.

## Содержание папки

| Файл | Описание |
|------|----------|
| `stopwatch_measure.csx` | Измерение времени выполнения с помощью `Stopwatch` (прогрев, усреднение, сравнение методов). |
| `memory_measure.csx` | Измерение аллокаций через `GC.GetTotalAllocatedBytes`. |
| `gc_counters.csx` | Счётчики GC: поколения, LOH, NoGCRegion. |
| `event_listener.csx` | Прослушивание событий .NET Runtime через `EventListener` (GC, ThreadPool). |
| `benchmarkdotnet_example.csx` | Пример использования BenchmarkDotNet в скрипте. |

---

## Внешние инструменты для профилирования

### 1. BenchmarkDotNet (библиотека для бенчмаркинга)

**Установка** (в отдельный проект): ```dotnet add package BenchmarkDotNet```

#### Базовый пример (класс с атрибутами [Benchmark]):
```
[MemoryDiagnoser]
public class MyBenchmark
{
    [Benchmark(Baseline = true)]
    public void MethodA() { ... }
    [Benchmark]
    public void MethodB() { ... }
}
```

**Запуск**: ```BenchmarkRunner.Run<MyBenchmark>();```

**Результаты** выводятся в консоль и сохраняются в HTML/Markdown.

**Документация**: [BenchmarkDotNet Official](https://benchmarkdotnet.org/)


### 2. PerfView (бесплатный инструмент от Microsoft)

**Скачать** [PerfView on GitHub](https://github.com/microsoft/perfview)

**Основные сценарии**:

- Сбор ETW-событий (GC, JIT, ThreadPool, исключения).
- Анализ времени выполнения (CPU stacks).
- Поиск утечек памяти (Heap dump).

**Типичные команды**:

- Запуск сбора: ```PerfView.exe collect MyApp.exe```
- Анализ: открыть `.etl` файл, перейти на вкладку `GC` или `CPU Stacks`.

**Документация**: [PerfView Tutorial](https://github.com/microsoft/perfview)


### 3. dotTrace (платный, от JetBrains)

**Скачивание**: [JetBrains dotTrace](https://www.jetbrains.com/profiler/)

**Основные сценарии**:

- Профилирование CPU (время выполнения методов).
- Профилирование памяти (аллокации, объекты, GC).
- Анализ производительности на уровне строк кода.

**Типичные команды** (в командной строке): ```dotTrace.exe start --save-to=snapshot.dtp -- MyApp.exe```

**Документация:** [dotTrace Help](https://www.jetbrains.com/help/profiler/dotTrace_Introduction.html)

### 4. ETW (Event Tracing for Windows) – встроенный механизм Windows

ETW – это системный уровень трассировки. В .NET используется через:

- `dotnet-trace` (кросс-платформенный)
- `PerfView` (под капотом)
- `EventListener` (в коде, см. пример)

**Установка**: `dotnet-trace`: ```dotnet tool install -g dotnet-trace```

**Сбор трассировки GC**: ```dotnet-trace collect --providers Microsoft-Windows-DotNETRuntime:0x1:4 -- MyApp.exe```

**Сбор всех событий**: ```dotnet-trace collect --providers Microsoft-Windows-DotNETRuntime -- MyApp.exe```

**Анализ**: Открыть `.nettrace` в `PerfView` или `через dotnet-trace dump`.



### 5. dotnet-counters (мониторинг в реальном времени)

**Установка**: ```dotnet tool install -g dotnet-counters```

**Мониторинг GC**: ```dotnet-counters monitor --process-id <PID> System.GC```

**Мониторинг всех счетчиков**: ```dotnet-counters monitor --process-id <PID>```

**Документация**: [Документация](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)



## Рекомендации по выбору инструмента

| Задача | Инструмент |
|--------|------------|
| Быстрое измерение времени в коде | `Stopwatch` (`.csx`) |
| Сравнение производительности нескольких реализаций | **BenchmarkDotNet** |
| Диагностика GC, памяти, аллокаций | **PerfView** или **dotnet-trace** |
| Детальный анализ CPU (стек вызовов) | **dotTrace** или **PerfView** |
| Мониторинг в реальном времени без остановки | **dotnet-counters** |
| Прослушивание событий из кода | `EventListener` (`.csx`) |













