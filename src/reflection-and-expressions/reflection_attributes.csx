#nullable disable

using System;
using System.Reflection;
using System.Linq;



// 1. Объявление кастомного атрибута
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
class DescriptionAttribute : Attribute
{
    public string Text { get; }
    public int Priority { get; set; } // именованный параметр
    public DescriptionAttribute(string text) => Text = text;
}


// 2. Объявление другого атрибута для демонстрации (с именованными полями)
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class VersionAttribute : Attribute
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public VersionAttribute(int major, int minor) { Major = major; Minor = minor; }
}


// 3. Применение атрибутов к классам, методам, свойствам
[Description("Основной класс примера", Priority = 1)]
[Version(1, 0)] // исправлено: передаём оба аргумента позиционно
class SampleClass
{
    [Description("Публичное свойство", Priority = 2)]
    public int Id { get; set; }

    [Description("Метод для демонстрации", Priority = 3)]
    public void DoWork(string name)
    {
        Console.WriteLine($"   Выполняется работа с именем: {name}");
    }

    [Description("Приватный метод", Priority = 4)]
    private void InternalHelper() { }
}


// 4. Чтение атрибутов типа (класса)
Console.WriteLine("1. Атрибуты класса SampleClass:");

Type sampleType = typeof(SampleClass);

// GetCustomAttributes – все атрибуты
var allClassAttrs = sampleType.GetCustomAttributes(false);
Console.WriteLine($"   Всего атрибутов: {allClassAttrs.Length}");

// DescriptionAttribute
var descAttr = sampleType.GetCustomAttribute<DescriptionAttribute>();
if (descAttr != null)
    Console.WriteLine($"   Description: {descAttr.Text}, Priority={descAttr.Priority}");

var versionAttr = sampleType.GetCustomAttribute<VersionAttribute>();
if (versionAttr != null)
    Console.WriteLine($"   Version: {versionAttr.Major}.{versionAttr.Minor}");

// Проверка наличия без извлечения
bool hasDesc = sampleType.IsDefined(typeof(DescriptionAttribute), inherit: false);
Console.WriteLine($"   IsDefined(Description): {hasDesc}");
Console.WriteLine();


// 5. Чтение атрибутов свойств и методов
Console.WriteLine("2. Атрибуты членов:");

// Свойство Id
PropertyInfo idProp = sampleType.GetProperty("Id");
var propDesc = idProp.GetCustomAttribute<DescriptionAttribute>();
if (propDesc != null)
    Console.WriteLine($"   Свойство Id: {propDesc.Text}, Priority={propDesc.Priority}");

// Метод DoWork
MethodInfo doWorkMethod = sampleType.GetMethod("DoWork");
var methodDesc = doWorkMethod.GetCustomAttribute<DescriptionAttribute>();
if (methodDesc != null)
    Console.WriteLine($"   Метод DoWork: {methodDesc.Text}, Priority={methodDesc.Priority}");

// Приватный метод – тоже можно, если использовать флаги
MethodInfo privateMethod = sampleType.GetMethod("InternalHelper", BindingFlags.NonPublic | BindingFlags.Instance);
var privateDesc = privateMethod.GetCustomAttribute<DescriptionAttribute>();
if (privateDesc != null)
    Console.WriteLine($"   Приватный метод InternalHelper: {privateDesc.Text}, Priority={privateDesc.Priority}");
Console.WriteLine();


// 6. Поиск всех членов с определённым атрибутом
Console.WriteLine("3. Все члены с атрибутом Description:");

var allMembers = sampleType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

foreach (MemberInfo member in allMembers)
{
    var attrs = member.GetCustomAttributes<DescriptionAttribute>();
    foreach (var attr in attrs)
    {
        Console.WriteLine($"   {member.MemberType} {member.Name}: {attr.Text} (Priority={attr.Priority})");
    }
}
Console.WriteLine();


// 7. Атрибуты с AllowMultiple = true (можно несколько на одном элементе)
[Description("Первое описание", Priority = 5)]
[Description("Второе описание", Priority = 10)]
class MultiAttributedClass { }

Console.WriteLine("4. Несколько атрибутов на одном классе:");

Type multiType = typeof(MultiAttributedClass);
var multiDescs = multiType.GetCustomAttributes<DescriptionAttribute>();
int idx = 1;
foreach (var d in multiDescs)
{
    Console.WriteLine($"   #{idx}: {d.Text}, Priority={d.Priority}");
    idx++;
}
Console.WriteLine();


// 8. Атрибуты на сборке (не в скрипте, но покажем принцип)
Console.WriteLine("5. Атрибуты на сборке (информационные):");
// Для текущей сборки можно получить атрибуты
var assemblyAttrs = Assembly.GetExecutingAssembly().GetCustomAttributes();
if (assemblyAttrs.Any())
{
    foreach (var attr in assemblyAttrs)
        Console.WriteLine($"   {attr.GetType().Name}");
}
else
{
    Console.WriteLine("   Атрибутов сборки нет (обычно в скриптах они не добавляются).");
}
Console.WriteLine();


// 9. Практический пример: использование атрибута для описания
Console.WriteLine("6. Демонстрация: вывод описания всех методов класса SampleClass (только публичных)");

var publicMethods = sampleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
foreach (var method in publicMethods)
{
    var desc = method.GetCustomAttribute<DescriptionAttribute>();
    string descText = desc != null ? $"[{desc.Text}]" : "[без описания]";
    Console.WriteLine($"   {method.Name} {descText}");
}
Console.WriteLine();
