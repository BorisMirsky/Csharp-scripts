#nullable disable

using System;
using System.Linq.Expressions;
using System.Reflection;


// - Expression Trees позволяют создавать делегаты для чтения/записи полей и свойств.
// - Это быстрее, чем PropertyInfo.GetValue/SetValue, т.к. делегат вызывается напрямую.
// - Для свойств используем Expression.Property + Expression.Lambda.
// - Для полей используем Expression.Field.
// - Для приватных членов нужно указать BindingFlags.NonPublic при получении MemberInfo.
// - Полученные делегаты можно кэшировать в словаре для повторного использования.



// 1. Демонстрационный класс с разными членами
class Sample
{
    public int PublicProperty { get; set; }
    public string PublicField;
    private int PrivateProperty { get; set; }
    private string PrivateField;

    public Sample(int publicProp, string publicField, int privateProp, string privateField)
    {
        PublicProperty = publicProp;
        PublicField = publicField;
        PrivateProperty = privateProp;
        PrivateField = privateField;
    }

    public override string ToString() =>
        $"PublicProperty={PublicProperty}, PublicField={PublicField}, PrivateProperty={PrivateProperty}, PrivateField={PrivateField}";
}


// 2. Генерация делегата для чтения публичного свойства
Console.WriteLine("1. Генерация getter для публичного свойства:");

// Создаём параметр-цель
ParameterExpression target = Expression.Parameter(typeof(Sample), "obj");

// Получаем PropertyInfo для PublicProperty
PropertyInfo propInfo = typeof(Sample).GetProperty("PublicProperty");
// Строим выражение доступа к свойству
MemberExpression propAccess = Expression.Property(target, propInfo);
// Создаём лямбду: (Sample obj) => obj.PublicProperty
Expression<Func<Sample, int>> getPropLambda = Expression.Lambda<Func<Sample, int>>(propAccess, target);
// Компилируем в делегат
Func<Sample, int> getProp = getPropLambda.Compile();

// Тестируем
var obj = new Sample(42, "Hello", 100, "Secret");
int value = getProp(obj);
Console.WriteLine($"   PublicProperty = {value}");
Console.WriteLine();


// 3. Генерация делегата для записи публичного свойства (setter)
Console.WriteLine("2. Генерация setter для публичного свойства:");

// Параметры: цель и новое значение
ParameterExpression target2 = Expression.Parameter(typeof(Sample), "obj");
ParameterExpression newValue = Expression.Parameter(typeof(int), "value");

// Выражение присваивания
MemberExpression propAccess2 = Expression.Property(target2, propInfo);
BinaryExpression assignExpr = Expression.Assign(propAccess2, newValue);
// Создаём лямбду: (Sample obj, int value) => obj.PublicProperty = value
Expression<Action<Sample, int>> setPropLambda = Expression.Lambda<Action<Sample, int>>(assignExpr, target2, newValue);
// Компилируем в делегат
Action<Sample, int> setProp = setPropLambda.Compile();

// Тестируем
setProp(obj, 99);
Console.WriteLine($"   После установки PublicProperty = {obj.PublicProperty}");
Console.WriteLine();


// 4. Генерация делегата для чтения публичного поля
Console.WriteLine("3. Генерация getter для публичного поля:");

FieldInfo fieldInfo = typeof(Sample).GetField("PublicField");
MemberExpression fieldAccess = Expression.Field(target, fieldInfo);
Expression<Func<Sample, string>> getFieldLambda = Expression.Lambda<Func<Sample, string>>(fieldAccess, target);
Func<Sample, string> getField = getFieldLambda.Compile();

string fieldVal = getField(obj);
Console.WriteLine($"   PublicField = {fieldVal}");
Console.WriteLine();


// 5. Генерация делегата для записи публичного поля
Console.WriteLine("4. Генерация setter для публичного поля:");

ParameterExpression newString = Expression.Parameter(typeof(string), "value");
MemberExpression fieldAccess2 = Expression.Field(target, fieldInfo);
BinaryExpression assignField = Expression.Assign(fieldAccess2, newString);
Expression<Action<Sample, string>> setFieldLambda = Expression.Lambda<Action<Sample, string>>(assignField, target, newString);
Action<Sample, string> setField = setFieldLambda.Compile();

setField(obj, "World");
Console.WriteLine($"   После установки PublicField = {obj.PublicField}");
Console.WriteLine();


// 6. Генерация делегата для доступа к приватному свойству (через BindingFlags.NonPublic)
Console.WriteLine("5. Генерация getter для приватного свойства:");

PropertyInfo privatePropInfo = typeof(Sample).GetProperty("PrivateProperty", BindingFlags.NonPublic | BindingFlags.Instance);
// Приватное свойство: выражение доступа такое же
MemberExpression privatePropAccess = Expression.Property(target, privatePropInfo);
Expression<Func<Sample, int>> getPrivatePropLambda = Expression.Lambda<Func<Sample, int>>(privatePropAccess, target);
Func<Sample, int> getPrivateProp = getPrivatePropLambda.Compile();

int privateVal = getPrivateProp(obj);
Console.WriteLine($"   PrivateProperty (приватное) = {privateVal}");

// Также можно сгенерировать setter для приватного свойства
ParameterExpression newPrivateValue = Expression.Parameter(typeof(int), "value");
MemberExpression privatePropAccess2 = Expression.Property(target, privatePropInfo);
BinaryExpression assignPrivate = Expression.Assign(privatePropAccess2, newPrivateValue);
Expression<Action<Sample, int>> setPrivatePropLambda = Expression.Lambda<Action<Sample, int>>(assignPrivate, target, newPrivateValue);
Action<Sample, int> setPrivateProp = setPrivatePropLambda.Compile();
setPrivateProp(obj, 999);
Console.WriteLine($"   После установки PrivateProperty = {getPrivateProp(obj)}");
Console.WriteLine();


// 7. Генерация делегата для доступа к приватному полю
Console.WriteLine("6. Генерация getter/setter для приватного поля:");

FieldInfo privateFieldInfo = typeof(Sample).GetField("PrivateField", BindingFlags.NonPublic | BindingFlags.Instance);
MemberExpression privateFieldAccess = Expression.Field(target, privateFieldInfo);
Expression<Func<Sample, string>> getPrivateFieldLambda = Expression.Lambda<Func<Sample, string>>(privateFieldAccess, target);
Func<Sample, string> getPrivateField = getPrivateFieldLambda.Compile();

Console.WriteLine($"   PrivateField до изменения = {getPrivateField(obj)}");

// Setter для приватного поля
ParameterExpression newPrivateField = Expression.Parameter(typeof(string), "value");
MemberExpression privateFieldAccess2 = Expression.Field(target, privateFieldInfo);
BinaryExpression assignPrivateField = Expression.Assign(privateFieldAccess2, newPrivateField);
Expression<Action<Sample, string>> setPrivateFieldLambda = Expression.Lambda<Action<Sample, string>>(assignPrivateField, target, newPrivateField);
Action<Sample, string> setPrivateField = setPrivateFieldLambda.Compile();

setPrivateField(obj, "Updated Secret");
Console.WriteLine($"   После установки PrivateField = {getPrivateField(obj)}");
Console.WriteLine();


// 8. Кэширование делегатов (пример простого словаря)
Console.WriteLine("7. Кэширование делегатов (упрощённо):");
// Словарь для хранения геттеров по имени свойства
var getterCache = new System.Collections.Generic.Dictionary<string, Func<Sample, object>>();

// Функция для получения или создания геттера для свойства
Func<Sample, object> GetOrCreateGetter(string propertyName)
{
    if (!getterCache.ContainsKey(propertyName))
    {
        PropertyInfo pi = typeof(Sample).GetProperty(propertyName);
        if (pi == null) throw new ArgumentException($"Property '{propertyName}' not found");
        ParameterExpression p = Expression.Parameter(typeof(Sample), "p");
        MemberExpression member = Expression.Property(p, pi);
        // Возвращаем object, чтобы можно было хранить разные типы
        var converted = Expression.Convert(member, typeof(object));
        var lambda = Expression.Lambda<Func<Sample, object>>(converted, p);
        getterCache[propertyName] = lambda.Compile();
    }
    return getterCache[propertyName];
}

// Первый вызов - создаёт и кэширует
var getter1 = GetOrCreateGetter("PublicProperty");
object cachedValue1 = getter1(obj);
Console.WriteLine($"   Из кэша (первый раз): PublicProperty = {cachedValue1}");

// Второй вызов - берёт из кэша
var getter2 = GetOrCreateGetter("PublicProperty");
object cachedValue2 = getter2(obj);
Console.WriteLine($"   Из кэша (второй раз): PublicProperty = {cachedValue2}");
Console.WriteLine("   (кэш работает, делегат не пересоздаётся)");
Console.WriteLine();


