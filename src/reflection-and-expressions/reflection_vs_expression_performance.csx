#nullable disable

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

// ==================================================================
// СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ: Прямой вызов vs Reflection vs Expression
// ==================================================================
// Теория коротко:
// - Прямой вызов – самый быстрый (компилятор знает всё на этапе компиляции).
// - Reflection (MethodInfo.Invoke, PropertyInfo.GetValue) – медленный из-за проверок типов,
//   упаковки параметров, динамического поиска.
// - Expression Trees (скомпилированный делегат) – почти так же быстр, как прямой вызов,
//   т.к. генерирует IL-код, аналогичный прямому вызову (без лишних проверок).
// - Разница может быть в 10–100 раз между рефлексией и прямым вызовом.
// ==================================================================

Console.WriteLine("=== Сравнение производительности: прямой вызов vs Reflection vs Expression ===\n");

// -------------------------------------------------------------
// 1. Тестовый класс
// -------------------------------------------------------------
class TestClass
{
    public int Value { get; set; }
    public int Add(int a, int b) => a + b;
}

// -------------------------------------------------------------
// 2. Подготовка объектов и делегатов
// -------------------------------------------------------------
TestClass testObj = new TestClass { Value = 42 };

// Прямой вызов – для baseline
int DirectMethodCall(int a, int b) => testObj.Add(a, b);
int DirectPropertyGet() => testObj.Value;
void DirectPropertySet(int val) => testObj.Value = val;

// Reflection
Type testType = typeof(TestClass);
MethodInfo addMethod = testType.GetMethod("Add");
PropertyInfo valueProp = testType.GetProperty("Value");

// Expression для метода
ParameterExpression objParam = Expression.Parameter(typeof(TestClass), "obj");
ParameterExpression paramA = Expression.Parameter(typeof(int), "a");
ParameterExpression paramB = Expression.Parameter(typeof(int), "b");
MethodCallExpression callExpr = Expression.Call(objParam, addMethod, paramA, paramB);
var methodLambda = Expression.Lambda<Func<TestClass, int, int, int>>(callExpr, objParam, paramA, paramB);
Func<TestClass, int, int, int> expressionMethod = methodLambda.Compile();

// Expression для свойства (геттер)
MemberExpression propAccess = Expression.Property(objParam, valueProp);
var getPropLambda = Expression.Lambda<Func<TestClass, int>>(propAccess, objParam);
Func<TestClass, int> expressionGetProp = getPropLambda.Compile();

// Expression для свойства (сеттер)
ParameterExpression newValParam = Expression.Parameter(typeof(int), "val");
BinaryExpression assignExpr = Expression.Assign(propAccess, newValParam);
var setPropLambda = Expression.Lambda<Action<TestClass, int>>(assignExpr, objParam, newValParam);
Action<TestClass, int> expressionSetProp = setPropLambda.Compile();

// -------------------------------------------------------------
// 3. Функция замера времени (с выводом)
// -------------------------------------------------------------
void Measure(string name, Action action, int iterations = 10_000_000)
{
    GC.Collect(); // стараемся минимизировать влияние GC на замеры
    GC.WaitForPendingFinalizers();
    Stopwatch sw = Stopwatch.StartNew();
    action();
    sw.Stop();
    Console.WriteLine($"{name,-35} {iterations,10:N0} итераций за {sw.ElapsedMilliseconds,6} мс");
}

// -------------------------------------------------------------
// 4. Замеры для метода Add
// -------------------------------------------------------------
Console.WriteLine("1. Вызов метода Add(int, int):");
const int N = 1_000_000;

// Warm-up
DirectMethodCall(1, 2);
addMethod.Invoke(testObj, new object[] { 1, 2 });
expressionMethod(testObj, 1, 2);

// Прямой вызов
Measure("   Прямой вызов", () =>
{
    int sum = 0;
    for (int i = 0; i < N; i++) sum = DirectMethodCall(i % 10, (i + 1) % 10);
}, N);

// Reflection
Measure("   MethodInfo.Invoke", () =>
{
    object sum = 0;
    for (int i = 0; i < N; i++) sum = addMethod.Invoke(testObj, new object[] { i % 10, (i + 1) % 10 });
}, N);

// Expression
Measure("   Expression делегат", () =>
{
    int sum = 0;
    for (int i = 0; i < N; i++) sum = expressionMethod(testObj, i % 10, (i + 1) % 10);
}, N);

Console.WriteLine();

// -------------------------------------------------------------
// 5. Замеры для свойства (геттер)
// -------------------------------------------------------------
Console.WriteLine("2. Чтение свойства Value (геттер):");

// Warm-up
int _ = DirectPropertyGet();
_ = (int)valueProp.GetValue(testObj);
_ = expressionGetProp(testObj);

Measure("   Прямой доступ", () =>
{
    int val = 0;
    for (int i = 0; i < N; i++) val = DirectPropertyGet();
}, N);

Measure("   PropertyInfo.GetValue", () =>
{
    int val = 0;
    for (int i = 0; i < N; i++) val = (int)valueProp.GetValue(testObj);
}, N);

Measure("   Expression делегат", () =>
{
    int val = 0;
    for (int i = 0; i < N; i++) val = expressionGetProp(testObj);
}, N);

Console.WriteLine();

// -------------------------------------------------------------
// 6. Замеры для свойства (сеттер)
// -------------------------------------------------------------
Console.WriteLine("3. Запись свойства Value (сеттер):");

// Warm-up
DirectPropertySet(0);
valueProp.SetValue(testObj, 0);
expressionSetProp(testObj, 0);

Measure("   Прямой доступ", () =>
{
    for (int i = 0; i < N; i++) DirectPropertySet(i);
}, N);

Measure("   PropertyInfo.SetValue", () =>
{
    for (int i = 0; i < N; i++) valueProp.SetValue(testObj, i);
}, N);

Measure("   Expression делегат", () =>
{
    for (int i = 0; i < N; i++) expressionSetProp(testObj, i);
}, N);

Console.WriteLine();

// -------------------------------------------------------------
// 7. Дополнительно: создание объекта (Activator vs new)
// -------------------------------------------------------------
Console.WriteLine("4. Создание объекта (конструктор без параметров):");

class Dummy { public Dummy() { } }
ConstructorInfo ctorInfo = typeof(Dummy).GetConstructor(Type.EmptyTypes);
Func<object> ctorLambda = Expression.Lambda<Func<object>>(Expression.New(ctorInfo)).Compile();

Measure("   new Dummy()", () =>
{
    for (int i = 0; i < N/10; i++) { var d = new Dummy(); }
}, N/10);

Measure("   Activator.CreateInstance", () =>
{
    for (int i = 0; i < N/10; i++) { var d = Activator.CreateInstance(typeof(Dummy)); }
}, N/10);

Measure("   Expression.New", () =>
{
    for (int i = 0; i < N/10; i++) { var d = ctorLambda(); }
}, N/10);

Console.WriteLine();

// -------------------------------------------------------------
// 8. Вывод-резюме
// -------------------------------------------------------------
Console.WriteLine("=== Выводы ===");
Console.WriteLine("- Прямой вызов – самый быстрый (эталон).");
Console.WriteLine("- Expression (скомпилированный делегат) практически не уступает прямому вызову.");
Console.WriteLine("- Reflection (Invoke/GetValue/SetValue) значительно медленнее – обычно в 10–100 раз.");
Console.WriteLine("- Используйте Expression для динамического доступа, если нужна высокая производительность.");
Console.WriteLine("- Рефлексия допустима, если вызовы редки (например, в DI-контейнерах при старте).");
Console.WriteLine();
