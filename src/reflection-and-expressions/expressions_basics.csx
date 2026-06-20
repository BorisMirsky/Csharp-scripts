#nullable disable

using System;
using System.Linq.Expressions;
using System.Reflection; 


// - Expression Trees представляют код в виде данных (дерева узлов).
// - Позволяют анализировать, модифицировать и компилировать код во время выполнения.
// - Используются в LINQ to SQL/Entity Framework (IQueryable), сериализации, динамическом коде.
// - Основные типы: ParameterExpression, ConstantExpression, BinaryExpression, LambdaExpression.
// - Компиляция в делегат: Expression.Lambda<Func<...>>(...).Compile().
// - Отличаются от рефлексии: рефлексия работает с существующими типами/членами, а выражения строят новый код.



// 1. Простейшее выражение – константа
Console.WriteLine("1. Константное выражение:");
ConstantExpression constExpr = Expression.Constant(42);
Console.WriteLine($"   Тип: {constExpr.NodeType}");
Console.WriteLine($"   Значение: {constExpr.Value}");
Console.WriteLine($"   Тип значения: {constExpr.Type}");
Console.WriteLine();


// 2. Выражение с параметром и операцией
Console.WriteLine("2. Выражение с параметром (x => x + 5):");
ParameterExpression paramX = Expression.Parameter(typeof(int), "x");
ConstantExpression five = Expression.Constant(5);
BinaryExpression addExpr = Expression.Add(paramX, five);
Expression<Func<int, int>> lambda = Expression.Lambda<Func<int, int>>(addExpr, paramX);
Console.WriteLine($"   Выражение: {lambda}");
Console.WriteLine($"   Body: {lambda.Body}");
Console.WriteLine($"   Parameters: {string.Join(", ", lambda.Parameters)}");
Console.WriteLine($"   Return type: {lambda.ReturnType}");
Console.WriteLine();

Func<int, int> func = lambda.Compile();
int result = func(10);
Console.WriteLine($"   Результат вызова (10) => {result}");
Console.WriteLine();


// 3. Выражение с двумя параметрами: (a, b) => a * b
Console.WriteLine("3. Выражение с двумя параметрами: (a, b) => a * b");
ParameterExpression paramA = Expression.Parameter(typeof(int), "a");
ParameterExpression paramB = Expression.Parameter(typeof(int), "b");
BinaryExpression multiplyExpr = Expression.Multiply(paramA, paramB);
Expression<Func<int, int, int>> lambda2 = Expression.Lambda<Func<int, int, int>>(multiplyExpr, paramA, paramB);
Func<int, int, int> multiplyFunc = lambda2.Compile();
int prod = multiplyFunc(6, 7);
Console.WriteLine($"   6 * 7 = {prod}");
Console.WriteLine();


// 4. Разбор структуры выражения
Console.WriteLine("4. Разбор выражения (показать дерево):");
void DumpExpression(Expression expr, int indent = 0)
{
    string pad = new string(' ', indent * 2);
    Console.WriteLine($"{pad}NodeType: {expr.NodeType}, Type: {expr.Type}");
    switch (expr)
    {
        case ConstantExpression c:
            Console.WriteLine($"{pad}  Value: {c.Value}");
            break;
        case ParameterExpression p:
            Console.WriteLine($"{pad}  Name: {p.Name}");
            break;
        case BinaryExpression b:
            Console.WriteLine($"{pad}  Left:");
            DumpExpression(b.Left, indent + 1);
            Console.WriteLine($"{pad}  Right:");
            DumpExpression(b.Right, indent + 1);
            break;
        case LambdaExpression l:
            Console.WriteLine($"{pad}  Parameters: {string.Join(", ", l.Parameters)}");
            Console.WriteLine($"{pad}  Body:");
            DumpExpression(l.Body, indent + 1);
            break;
        default:
            Console.WriteLine($"{pad}  (другие узлы не разбираем)");
            break;
    }
}

Expression<Func<int, int, int>> sampleLambda = (a, b) => (a + b) * 2;
Console.WriteLine("   Исходное выражение: (a, b) => (a + b) * 2");
DumpExpression(sampleLambda);
Console.WriteLine();


// 5. Условное выражение (Conditional)
Console.WriteLine("5. Условное выражение: (x) => x > 0 ? x : -x");
ParameterExpression paramX2 = Expression.Parameter(typeof(int), "x");
ConstantExpression zero = Expression.Constant(0);
BinaryExpression condition = Expression.GreaterThan(paramX2, zero);
ConditionalExpression ifThenElse = Expression.Condition(condition, paramX2, Expression.Negate(paramX2));
Expression<Func<int, int>> absLambda = Expression.Lambda<Func<int, int>>(ifThenElse, paramX2);
Func<int, int> absFunc = absLambda.Compile();
Console.WriteLine($"   Abs(-5) = {absFunc(-5)}");
Console.WriteLine($"   Abs(5) = {absFunc(5)}");
Console.WriteLine();


// 6. Вызов метода через Expression.Call
Console.WriteLine("6. Вызов метода (string.Contains) через Expression:");
ParameterExpression strParam = Expression.Parameter(typeof(string), "s");
ConstantExpression subStr = Expression.Constant("world");
MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
MethodCallExpression callExpr = Expression.Call(strParam, containsMethod, subStr);
Expression<Func<string, bool>> containsLambda = Expression.Lambda<Func<string, bool>>(callExpr, strParam);
Func<string, bool> containsFunc = containsLambda.Compile();
Console.WriteLine($"   \"Hello world\".Contains(\"world\") = {containsFunc("Hello world")}");
Console.WriteLine($"   \"Hello world\".Contains(\"foo\") = {containsFunc("Hello world")}");
Console.WriteLine();


// 7. Создание делегата для доступа к свойству (замена PropertyInfo.GetValue)
Console.WriteLine("7. Генерация делегата для доступа к свойству:");
class Person { public string Name { get; set; } }
ParameterExpression personParam = Expression.Parameter(typeof(Person), "p");
PropertyInfo nameProp = typeof(Person).GetProperty("Name");
MemberExpression nameAccess = Expression.MakeMemberAccess(personParam, nameProp);
Expression<Func<Person, string>> getNameLambda = Expression.Lambda<Func<Person, string>>(nameAccess, personParam);
Func<Person, string> getName = getNameLambda.Compile();
Person p = new Person { Name = "Alice" };
Console.WriteLine($"   Имя: {getName(p)}");
Console.WriteLine();


// 8. Изменение выражения через ExpressionVisitor (продвинуто, но покажем базово)
Console.WriteLine("8. Модификация выражения (умножение на 2 перед сложением):");
ParameterExpression aParam = Expression.Parameter(typeof(int), "a");
ParameterExpression bParam = Expression.Parameter(typeof(int), "b");
BinaryExpression sumExpr = Expression.Add(aParam, bParam);
Expression<Func<int, int, int>> original = Expression.Lambda<Func<int, int, int>>(sumExpr, aParam, bParam);
Console.WriteLine($"   Оригинал: {original}");
Console.WriteLine("   (Модификация опущена для краткости, см. документацию по ExpressionVisitor)");
Console.WriteLine();

