using System;
using System.IO;

internal class ConsoleProgress : IProgress<double>, IDisposable
{
    public ConsoleProgress()
    {
        writer = Console.Out;
    }

    public void Report(double progress) => Write($"{progress:P1}");

    public void Dispose() => EraseLast();

    private void EraseLast()
    {
        if (_lastLength > 0 && !Console.IsOutputRedirected)
        {
            Console.SetCursorPosition(_posX, _posY);
            writer.Write(new string(' ', _lastLength));
            Console.SetCursorPosition(_posX, _posY);
        }
    }

    private void Write(string text)
    {
        if (!Console.IsOutputRedirected)
        {
            EraseLast();
            writer.Write(text);
            _lastLength = text.Length;
        }
    }

    private readonly TextWriter writer;
    private readonly int _posX = Console.CursorLeft;
    private readonly int _posY = Console.CursorTop;

    private int _lastLength;
}
