# Csharp-scripts
Standalone C# scripts (.csx) – run with dotnet script, no projects required.

Коллекция скриптов на C# (`.csx`) для демонстрации и понимания некоторых техник языка C#.  
Каждый файл можно запустить независимо без создания проекта Visual Studio.


## Требования

- [.NET SDK 8.0 или выше](https://dotnet.microsoft.com/download)
- Утилита [`dotnet-script`](https://github.com/filipw/dotnet-script). 

Ставится глобально, т.е. в любом месте:  ```dotnet tool install -g dotnet-script```

## Запуск
Перейти в папку со скриптом и выполнить:

```dotnet script file.csx```