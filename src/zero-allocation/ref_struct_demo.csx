#nullable disable

using System;

// ref struct - структуры, живущие ТОЛЬКО на стеке

ref struct BufferWrapper
{
    private Span<byte> _buffer;
    public BufferWrapper(Span<byte> buffer) => _buffer = buffer;
    public void SetByte(int index, byte value) { if (index < _buffer.Length) _buffer[index] = value; }
    public byte GetByte(int index) => _buffer[index];
    public int Length => _buffer.Length;
}

ref struct TextParser
{
    private ReadOnlySpan<char> _text;
    private int _position;
    public TextParser(ReadOnlySpan<char> text) { _text = text; _position = 0; }
    public ReadOnlySpan<char> ReadWord()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++;
        int start = _position;
        while (_position < _text.Length && !char.IsWhiteSpace(_text[_position])) _position++;
        return _text.Slice(start, _position - start);
    }
    public bool HasMore => _position < _text.Length;
}

ref struct SpanEnumerator
{
    private readonly ReadOnlySpan<int> _span;
    private int _index;
    public SpanEnumerator(ReadOnlySpan<int> span) { _span = span; _index = -1; }
    public int Current => _span[_index];
    public bool MoveNext() { _index++; return _index < _span.Length; }
    public SpanEnumerator GetEnumerator() => this;
}

ref struct DoubleBufferWrapper<T>
{
    private readonly Span<T> _first;
    private readonly Span<T> _second;
    public DoubleBufferWrapper(Span<T> first, Span<T> second) { _first = first; _second = second; }
    public Span<T> this[int index] => index switch { 0 => _first, 1 => _second, _ => throw new IndexOutOfRangeException() };
}

void DemonstrateRefStruct()
{
    Console.WriteLine("=== ref struct демонстрация ===\n");

    // 1. Базовое использование
    Span<byte> stackBuffer = stackalloc byte[10];
    var wrapper = new BufferWrapper(stackBuffer);
    wrapper.SetByte(0, 0xAA);
    wrapper.SetByte(5, 0xFF);
    Console.WriteLine("1. BufferWrapper (ref struct) над stackalloc:");
    Console.WriteLine($"   byte[0] = 0x{wrapper.GetByte(0):X2}, byte[5] = 0x{wrapper.GetByte(5):X2}\n");

    // 2. TextParser
    string sentence = "Hello world from ref struct";
    var parser = new TextParser(sentence.AsSpan());
    Console.WriteLine("2. TextParser (ref struct) разбор строки без аллокаций:");
    while (parser.HasMore)
    {
        var word = parser.ReadWord();
        Console.WriteLine($"   Слово: '{word.ToString()}'");
    }
    Console.WriteLine();

    // 3. Ограничения (только текст)
    Console.WriteLine("3. Ограничения (эти строки не скомпилируются):");
    Console.WriteLine("   class MyClass { private BufferWrapper _wrapper; } // Ошибка CS8345");
    Console.WriteLine("   struct MyStruct { private BufferWrapper _wrapper; } // Ошибка CS8345");
    Console.WriteLine("   BufferWrapper[] arr = new BufferWrapper[10]; // Ошибка CS8343\n");

    // 4. Async/await
    Console.WriteLine("4. Ограничение: async/await");
    Console.WriteLine("   async Task ProcessAsync(BufferWrapper wrapper) { await Task.Delay(1); } // Ошибка CS1988\n");

    // 5. Лямбды
    Console.WriteLine("5. Ограничение: лямбды");
    Console.WriteLine("   Func<int> f = () => wrapper.Length; // Ошибка CS8916\n");

    // 6. SpanEnumerator
    ReadOnlySpan<int> numbers = stackalloc int[] { 10, 20, 30, 40, 50 };
    var enumerator = new SpanEnumerator(numbers);
    Console.WriteLine("6. SpanEnumerator (ref struct) без аллокаций в foreach:");
    foreach (var num in enumerator)
    {
        Console.WriteLine($"   {num}");
    }
    Console.WriteLine();

    // 7. Обычная структура не может содержать ref struct
    Console.WriteLine("7. Обычная структура НЕ может содержать поля ref struct:");
    Console.WriteLine("   struct Bad { public Span<byte> data; } // Ошибка CS8345");
    Console.WriteLine("   ref struct Good { public Span<byte> data; } // OK\n");

    // 8. DoubleBufferWrapper
    Span<int> first = stackalloc int[] { 1, 2, 3 };
    Span<int> second = stackalloc int[] { 4, 5, 6 };
    var doubleBuf = new DoubleBufferWrapper<int>(first, second);
    Console.WriteLine("8. DoubleBufferWrapper (ref struct) – два буфера без аллокаций:");
    Console.WriteLine($"   First[0] = {doubleBuf[0][0]}, Second[1] = {doubleBuf[1][1]}\n");

}

// Вызов локальной функции (единственный top-level statement)
DemonstrateRefStruct();