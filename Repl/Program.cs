using System;
using System.Diagnostics;

namespace Repl
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new ConsoleReader(new MyCompletionEngine());

            while (true)
            {
                Console.Write("demo>");
                var line = reader.ReadLine();
                if (line == ":q") return;
                Console.WriteLine(line);
            }
        }

        private class MyCompletionEngine : ICompletionEngine
        {
            private readonly char[] _tokenDelimiters = { ' ' };
            public ConsoleKeyInfo Trigger { get; } = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
            public string[] GetCompletions(string partial)
            {
                switch (partial)
                {
                    case "": return new[] { "foo", "foobar", "foobarbaz" };
                    case "f": return new[] { "oo", "oobar", "oobarbaz" };
                    case "fo": return new[] { "o", "obar", "obarbaz" };
                    case "foo": return new[] { "bar", "barbaz" };
                    case "foob": return new[] { "ar", "arbaz" };
                    case "fooba": return new[] { "r", "rbaz" };
                    case "foobar": return new[] { "baz" };
                    case "foobarb": return new[] { "az" };
                    case "foobarba": return new[] { "z" };
                    default: return null;
                }
            }
            public char[] GetTokenDelimiters() => _tokenDelimiters;
        }
    }
}
