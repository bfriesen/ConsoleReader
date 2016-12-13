# REPL ConsoleReader

_A replacement of Console.ReadLine() that enables pluggable auto-completion, undo/redo, command history, copy/paste via ctrl+c/ctrl+x/ctrl+v, and other common text editor keyboard shortcuts._

--------------------------------------------------------------------------------

To use, create an instance of the ConsoleReader class. The default constructor will not have auto-completion activated, but all other features are enabled.

```c#
ConsoleReader consoleReader = new ConsoleReader();
Console.Write("Enter some text: ");

// The user will be able to use standard keyboard shortcuts to input their line.
var line = consoleReader.ReadLine();

Console.WriteLine($"You entered: '{line}'");
```

To activate auto-completion, pass an implementation of the `ICompletionEngine` interface.

```c#
// The sample completion engine completes tokens named 'foo', 'foobar', and 'foobarbaz'.
// Activate completion by typing the tab key after partially typing any of the tokens.

var completionEngine = new MyCompletionEngine();
ConsoleReader consoleReader = new ConsoleReader(completionEngine);
Console.Write("Enter some text: ");

// Standard keyboard shortcuts along with auto-completion are available for the user.
var line = consoleReader.ReadLine();

Console.WriteLine($"You entered: '{line}'");

/* ... */

public class MyCompletionEngine : ICompletionEngine
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
```
