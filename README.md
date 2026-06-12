# Csharp-scripts
Standalone C# scripts (.csx) – run with dotnet script, no projects required.

Коллекция небольших самодостаточных скриптов на C# (`.csx`), демонстрирующих некоторые продвинутые техники языка C#.  
Каждый файл можно запустить независимо без создания проекта Visual Studio.
Скрипты подходят для быстрого экспериментирования, обучения или шпаргалки.

## Требования

- [.NET SDK 8.0 или выше](https://dotnet.microsoft.com/download)
- Утилита [`dotnet-script`](https://github.com/filipw/dotnet-script) (глобальная установка)

Глобальная (в любом месте) установка `dotnet-script`:  ```dotnet tool install -g dotnet-script```

## Запуск
Перейдите в папку со скриптом и выполните:

```dotnet script file.csx```