#nullable disable

using System;
using System.Reflection;



// 1. Создание демонстрационного класса

#pragma warning disable CS0649 // Поле не используется, но нужно для демонстрации
class Calculator
{
    public int X { get; set; }
    public int Y { get; set; }
    public int PublicField;
    private int _privateField;

    public Calculator() => X = Y = 0;
    public Calculator(int x, int y) { X = x; Y = y; }

    public int Add() => X + Y;
    public int Multiply(int factor) => (X + Y) * factor;

    private int PrivateAdd(int a, int b) => a + b;

    public string GetInfo() => $"X={X}, Y={Y}, PublicField={PublicField}, _privateField={_privateField}";

    public void SetPrivateField(int value) => _privateField = value;
}
#pragma warning restore CS0649


// 2. Создание экземпляра через Activator.CreateInstance - метод рефлексии, 
//             позволяет создавать экземпляры класса динамически во время работы программы (runtime)

Console.WriteLine("1. Создание объекта:");

Type calcType = typeof(Calculator);

// Без параметров
Calculator calc1 = (Calculator)Activator.CreateInstance(calcType);
Console.WriteLine($"   Без параметров: {calc1.GetInfo()}");

// С параметрами конструктора
Calculator calc2 = (Calculator)Activator.CreateInstance(calcType, 5, 7);
Console.WriteLine($"   С параметрами (5,7): {calc2.GetInfo()}");

// Альтернативный способ: через ConstructorInfo
ConstructorInfo ctor = calcType.GetConstructor(new[] { typeof(int), typeof(int) });
Calculator calc3 = (Calculator)ctor.Invoke(new object[] { 10, 20 });
Console.WriteLine($"   Через ConstructorInfo: {calc3.GetInfo()}");
Console.WriteLine();


// 3. Вызов публичного метода

Console.WriteLine("2. Вызов публичного метода:");

MethodInfo addMethod = calcType.GetMethod("Add");
int sum = (int)addMethod.Invoke(calc2, null); // без параметров
Console.WriteLine($"   Add() на объекте (5,7) = {sum}");

MethodInfo multiplyMethod = calcType.GetMethod("Multiply");
int product = (int)multiplyMethod.Invoke(calc2, new object[] { 3 });
Console.WriteLine($"   Multiply(3) на объекте (5,7) = {product}");
Console.WriteLine();


// 4. Вызов приватного метода
Console.WriteLine("3. Вызов приватного метода (PrivateAdd):");

MethodInfo privateMethod = calcType.GetMethod("PrivateAdd", BindingFlags.NonPublic | BindingFlags.Instance);
int privateSum = (int)privateMethod.Invoke(calc2, new object[] { 100, 200 });
Console.WriteLine($"   PrivateAdd(100,200) = {privateSum} (объект (5,7) не используется)");
Console.WriteLine();


// 5. Чтение и запись свойств и полей
Console.WriteLine("4. Чтение/запись свойств и полей:");

// Свойство
PropertyInfo xProp = calcType.GetProperty("X");
xProp.SetValue(calc2, 42);
int xVal = (int)xProp.GetValue(calc2);
Console.WriteLine($"   X после установки = {xVal}");

// Поле публичное
FieldInfo publicField = calcType.GetField("PublicField");
publicField.SetValue(calc2, 99);
int fieldVal = (int)publicField.GetValue(calc2);
Console.WriteLine($"   PublicField = {fieldVal}");

// Поле приватное (через рефлексию)
FieldInfo privateField = calcType.GetField("_privateField", BindingFlags.NonPublic | BindingFlags.Instance);
privateField.SetValue(calc2, 777);
int privVal = (int)privateField.GetValue(calc2);
Console.WriteLine($"   _privateField (установлено через рефлексию) = {privVal}");
Console.WriteLine($"   Итоговое состояние: {calc2.GetInfo()}");
Console.WriteLine();


// 6. InvokeMember – универсальный вызов
Console.WriteLine("5. InvokeMember (универсальный вызов):");

// Вызов метода Add
object result = calcType.InvokeMember(
    "Add",
    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
    null,
    calc2,
    null
);
Console.WriteLine($"   InvokeMember Add() = {result}");

// Вызов метода Multiply с параметрами
result = calcType.InvokeMember(
    "Multiply",
    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
    null,
    calc2,
    new object[] { 4 }
);
Console.WriteLine($"   InvokeMember Multiply(4) = {result}");

// Получение свойства X
object xValue = calcType.InvokeMember(
    "X",
    BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
    null,
    calc2,
    null
);
Console.WriteLine($"   InvokeMember Get X = {xValue}");

// Установка свойства Y
calcType.InvokeMember(
    "Y",
    BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance,
    null,
    calc2,
    new object[] { 123 }
);
object yValue = calcType.InvokeMember(
    "Y",
    BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
    null,
    calc2,
    null
);
Console.WriteLine($"   После установки Y = {yValue}");
Console.WriteLine();



// 6. Обработка исключений при динамическом вызове (ИСПРАВЛЕННЫЙ БЛОК)
Console.WriteLine("6. Обработка исключений:");

// Пример 1: метод не существует
try
{
    MethodInfo badMethod = calcType.GetMethod("NonExistentMethod");
    if (badMethod == null)
        throw new MissingMethodException("Метод NonExistentMethod не найден");
    badMethod.Invoke(calc2, null);
}
catch (MissingMethodException ex)
{
    Console.WriteLine($"   Перехвачено MissingMethodException: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"   Перехвачено другое исключение: {ex.GetType().Name}");
}

// Пример 2: вызов с неправильным типом параметра (передаём строку вместо int)
try
{
    MethodInfo multiply = calcType.GetMethod("Multiply");
    multiply.Invoke(calc2, new object[] { "abc" });
}
catch (ArgumentException ex)
{
    // Исключение выбрасывается при проверке типов до вызова метода
    Console.WriteLine($"   Перехвачено ArgumentException: {ex.Message}");
}
catch (TargetInvocationException ex)
{
    // Если бы исключение возникло внутри метода, оно было бы обёрнуто
    Console.WriteLine($"   TargetInvocationException: {ex.InnerException?.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"   Непредвиденное исключение: {ex.GetType().Name}");
}
Console.WriteLine();


// 7. Создание объекта через приватный конструктор
Console.WriteLine("7. Создание объекта через приватный конструктор (демонстрация)");
class PrivateCtorClass
{
    private PrivateCtorClass() { }
    public static PrivateCtorClass Create() => new PrivateCtorClass();
}

try
{
    var obj = Activator.CreateInstance(typeof(PrivateCtorClass));
}

catch (MissingMethodException ex)
{
    Console.WriteLine($"   Activator.CreateInstance не сработал: {ex.Message}");
}

// Через ConstructorInfo с флагом NonPublic
ConstructorInfo privateCtor = typeof(PrivateCtorClass).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
var privateObj = privateCtor.Invoke(null);
Console.WriteLine($"   Создан через приватный конструктор: {privateObj.GetType().Name}");
Console.WriteLine();

