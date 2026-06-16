#nullable disable

using System;
using System.Reflection;
using System.Linq;


// 1. Получение Type разными способами
Console.WriteLine("1. Получение Type:");

// Способ 1: typeof(T)
Type stringType = typeof(string);
Console.WriteLine($"   typeof(string): {stringType.FullName}");

// Способ 2: obj.GetType()
string example = "Hello";
Type runtimeType = example.GetType();
Console.WriteLine($"   obj.GetType(): {runtimeType.FullName}");

// Способ 3: Type.GetType("полное_имя_типа") – должен быть в загруженной сборке
Type mathType = Type.GetType("System.Math", throwOnError: true);
Console.WriteLine($"   Type.GetType('System.Math'): {mathType.FullName}");

// Способ 4: для своего класса (объявим простой класс)
class MyClass { public int X { get; set; } }
Type myType = typeof(MyClass);
Console.WriteLine($"   typeof(MyClass): {myType.FullName}");
Console.WriteLine();


// 2. Информация о сборке (Assembly)
Console.WriteLine("2. Информация о сборке:");

// Текущая сборка (скрипт выполняется в динамической сборке)
Assembly currentAssembly = Assembly.GetExecutingAssembly();
Console.WriteLine($"   Имя: {currentAssembly.GetName().Name}");
Console.WriteLine($"   Версия: {currentAssembly.GetName().Version}");
Console.WriteLine($"   Расположение: {currentAssembly.Location}");

// Сборка, содержащая System.String (mscorlib или System.Private.CoreLib)
Assembly stringAssembly = typeof(string).Assembly;
Console.WriteLine($"   Сборка для string: {stringAssembly.GetName().Name}");
Console.WriteLine();


// 3. Обзор типа: члены (методы, свойства, поля, конструкторы)
Console.WriteLine("3. Обзор типа (на примере MyClass):");

// Создаём демонстрационный класс – подавляем предупреждения о неиспользуемых полях
#pragma warning disable CS0649, CS0169
class Demo
{
    public int PublicField;
    private string _privateField;
    public int PublicProperty { get; set; }
    private int PrivateProperty { get; set; }
    public Demo(int x) { PublicProperty = x; }
    public void PublicMethod() { }
    private void PrivateMethod() { }
}
#pragma warning restore CS0649, CS0169

Type demoType = typeof(Demo);

// Конструкторы
Console.WriteLine("   Конструкторы:");
foreach (ConstructorInfo ctor in demoType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
{
    Console.WriteLine($"      {ctor} (IsPublic={ctor.IsPublic})");
}

// Методы (только публичные, статические и экземплярные)
Console.WriteLine("   Методы (публичные):");
foreach (MethodInfo method in demoType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
{
    Console.WriteLine($"      {method.Name} ({method.ReturnType.Name})");
}

// Свойства (публичные)
Console.WriteLine("   Свойства (публичные):");
foreach (PropertyInfo prop in demoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
{
    Console.WriteLine($"      {prop.Name} ({prop.PropertyType.Name})");
}

// Поля (публичные)
Console.WriteLine("   Поля (публичные):");
foreach (FieldInfo field in demoType.GetFields(BindingFlags.Public | BindingFlags.Instance))
{
    Console.WriteLine($"      {field.Name} ({field.FieldType.Name})");
}

// Теперь включаем private члены
Console.WriteLine("   Все члены (включая private):");
BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
foreach (MemberInfo member in demoType.GetMembers(allFlags))
{
    Console.WriteLine($"      {member.MemberType} {member.Name}");
}
Console.WriteLine();


// 4. Атрибуты типа (подавляем устаревшее предупреждение)
Console.WriteLine("4. Атрибуты типа:");
#pragma warning disable SYSLIB0050 // Type.IsSerializable устарел
bool isSerializable = demoType.IsSerializable;
#pragma warning restore SYSLIB0050
bool isAbstract = demoType.IsAbstract;
bool isClass = demoType.IsClass;
Console.WriteLine($"   IsSerializable: {isSerializable} (устаревшее свойство)");
Console.WriteLine($"   IsAbstract: {isAbstract}");
Console.WriteLine($"   IsClass: {isClass}");
Console.WriteLine($"   BaseType: {demoType.BaseType?.Name ?? "(null)"}");

// Проверяем, реализует ли тип интерфейс IDisposable (пример)
bool implementsIDisposable = typeof(IDisposable).IsAssignableFrom(demoType);
Console.WriteLine($"   Implements IDisposable: {implementsIDisposable}");
Console.WriteLine();


// 5. Информация о сборке – список всех публичных типов
Console.WriteLine("5. Публичные типы в текущей сборке (первые 10):");
var publicTypes = currentAssembly.GetExportedTypes();
foreach (Type t in publicTypes.Take(10))
{
    Console.WriteLine($"   {t.FullName}");
}
if (publicTypes.Length > 10)
    Console.WriteLine($"   ... и ещё {publicTypes.Length - 10} типов");
Console.WriteLine();


// 6. Получение типа по строке с учётом сборки (дополнительно)
Console.WriteLine("6. Поиск типа по полному имени (сборка не указана):");
string typeName = "System.Int32";
Type foundType = Type.GetType(typeName);
if (foundType != null)
    Console.WriteLine($"   Найден: {foundType.FullName}");
else
    Console.WriteLine("   Тип не найден.");

// Если нужно из конкретной сборки – указываем имя сборки через запятую
string assemblyQualifiedName = "System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
Type foundQualified = Type.GetType(assemblyQualifiedName);
Console.WriteLine($"   С указанием сборки: {foundQualified?.FullName ?? "не найден"}");
Console.WriteLine();



