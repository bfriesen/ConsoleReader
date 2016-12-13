using System;

namespace Repl
{
    public interface ICompletionEngine
    {
        ConsoleKeyInfo Trigger { get; }
        string[] GetCompletions(string partial);
        char[] GetTokenDelimiters();
    }
}
