using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Repl
{
    public class ConsoleReader
    {
        private readonly ICompletionEngine _completionEngine;
        private readonly char[] _tokenDelimiters;

        public ConsoleReader() : this(new EmptyCompletionEngine()) { }
        public ConsoleReader(ICompletionEngine completionEngine)
        {
            if (completionEngine == null) throw new ArgumentNullException("completionEngine");
            _completionEngine = completionEngine;
            _tokenDelimiters = completionEngine.GetTokenDelimiters();
            Console.TreatControlCAsInput = true;
        }

        public string ReadLine()
        {
            var startLeft = Console.CursorLeft;
            var startTop = Console.CursorTop;
            var buffer = new StringBuilder();

            var selection = new Selection(buffer, startLeft, startTop);

            string[] completionCandidates = null;
            int completionIndex = -1;

            while (true)
            {
                var bufferIndex = GetBufferIndexFromCursor(startLeft, startTop);
                var keyInfo = Console.ReadKey(true);

                if (completionCandidates != null)
                {
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.Escape:
                        case ConsoleKey.Backspace:
                        case ConsoleKey.Delete:
                            completionCandidates = null;
                            completionIndex = -1;
                            Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            break;
                        case ConsoleKey.Tab:
                            if (completionCandidates.Length < 2) break;
                            completionIndex++;
                            if (completionIndex == completionCandidates.Length) completionIndex = 0;
                            bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                            break;
                        case ConsoleKey.Spacebar:
                            completionCandidates = null;
                            completionIndex = -1;
                            selection.Reset(bufferIndex);
                            bufferIndex = Insert(" ", startLeft, startTop, buffer, selection, bufferIndex);
                            break;
                        case ConsoleKey.Enter:
                            completionCandidates = null;
                            completionIndex = -1;
                            selection.Reset(bufferIndex);
                            break;
                        default:
                            bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            bufferIndex = Insert(keyInfo.KeyChar.ToString(), startLeft, startTop, buffer, selection, bufferIndex);
                            completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                            break;
                    }
                }
                else
                {
                    if (keyInfo.Key == _completionEngine.Trigger.Key
                        && keyInfo.Modifiers == _completionEngine.Trigger.Modifiers)
                    {
                        completionIndex = 0;
                        completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                    }
                    else
                    {
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.LeftArrow:
                                if (bufferIndex == 0)
                                {
                                    if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0)
                                        selection.Reset(bufferIndex);
                                    break;
                                }
                                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                {
                                    bufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                    selection.Reset(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
                                {
                                    bufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                    selection.Resize(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                {
                                    bufferIndex--;
                                    selection.Resize(bufferIndex);
                                }
                                else
                                {
                                    bufferIndex--;
                                    selection.Reset(bufferIndex);
                                }
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                break;
                            case ConsoleKey.RightArrow:
                                if (bufferIndex == buffer.Length)
                                {
                                    if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0)
                                        selection.Reset(bufferIndex);
                                    break;
                                }
                                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                {
                                    bufferIndex = GetNextWordBufferIndex(buffer, bufferIndex);
                                    selection.Reset(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
                                {
                                    bufferIndex = GetNextWordBufferIndex(buffer, bufferIndex);
                                    selection.Resize(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                {
                                    bufferIndex++;
                                    selection.Resize(bufferIndex);
                                }
                                else
                                {
                                    bufferIndex++;
                                    selection.Reset(bufferIndex);
                                }
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                break;
                            case ConsoleKey.Backspace:
                                if (bufferIndex == 0) break;
                                if (selection.Length > 0)
                                {
                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(new string(' ', buffer.Length));

                                    buffer.Remove(selection.Start, selection.Length);
                                    bufferIndex = selection.Start;

                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(buffer.ToString());
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                else
                                {
                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(new string(' ', buffer.Length));
                                    if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                    {
                                        var newBufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                        while (bufferIndex > newBufferIndex)
                                        {
                                            bufferIndex--;
                                            buffer.Remove(bufferIndex, 1);
                                        }
                                    }
                                    else
                                    {
                                        bufferIndex--;
                                        buffer.Remove(bufferIndex, 1);
                                    }
                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(buffer.ToString());
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                    selection.Reset(bufferIndex);
                                }
                                break;
                            case ConsoleKey.Delete:
                                if (bufferIndex == buffer.Length && selection.Length == 0) break;
                                bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                                break;
                            case ConsoleKey.Home:
                                bufferIndex = 0;
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                    selection.Resize(bufferIndex);
                                else selection.Reset(bufferIndex);
                                break;
                            case ConsoleKey.End:
                                bufferIndex = buffer.Length;
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                    selection.Resize(bufferIndex);
                                else selection.Reset(bufferIndex);
                                break;
                            case ConsoleKey.Enter:
                                Console.WriteLine();
                                return buffer.ToString();
                            default:
                                if (char.IsControl(keyInfo.KeyChar))
                                {
                                    switch (keyInfo.Key)
                                    {
                                        case ConsoleKey.A:
                                            selection.Reset(0);
                                            selection.Resize(buffer.Length);
                                            bufferIndex = buffer.Length;
                                            SetCursorPosition(bufferIndex, startLeft, startTop);
                                            break;
                                        case ConsoleKey.C:
                                            if (selection.Length == 0)
                                                Clipboard.Set(buffer.ToString());
                                            else
                                                Clipboard.Set(buffer.ToString(selection.Start, selection.Length));
                                            break;
                                        case ConsoleKey.X:
                                            if (selection.Length > 0)
                                            {
                                                Clipboard.Set(buffer.ToString(selection.Start, selection.Length));

                                                Console.SetCursorPosition(startLeft, startTop);
                                                Console.Write(new string(' ', buffer.Length));

                                                buffer.Remove(selection.Start, selection.Length);
                                                bufferIndex = selection.Start;

                                                Console.SetCursorPosition(startLeft, startTop);
                                                Console.Write(buffer.ToString());
                                                selection.Reset(bufferIndex);
                                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                            }
                                            break;
                                        case ConsoleKey.V:
                                            bufferIndex = Insert(Clipboard.Get(), startLeft, startTop, buffer, selection, bufferIndex);
                                            break;
                                    }
                                    break;
                                }
                                if (selection.Length > 0)
                                {
                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(new string(' ', buffer.Length));

                                    buffer.Remove(selection.Start, selection.Length);
                                    bufferIndex = selection.Start;
                                    buffer.Insert(bufferIndex, keyInfo.KeyChar);

                                    Console.SetCursorPosition(startLeft, startTop);
                                    Console.Write(buffer.ToString());
                                    bufferIndex++;
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                else
                                {
                                    buffer.Insert(bufferIndex, keyInfo.KeyChar);
                                    bufferIndex++;
                                    Console.Write(keyInfo.KeyChar);
                                    Console.Write(buffer.ToString(bufferIndex, buffer.Length - bufferIndex));
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private string[] DisplayCompletion(int startLeft, int startTop, StringBuilder buffer, Selection selection, int completionIndex, ref int bufferIndex)
        {
            string[] completionCandidates;
            var stack = new Stack<char>();
            var index = bufferIndex;
            while (index > 0 && _tokenDelimiters.All(t => t != buffer[index - 1]))
            {
                stack.Push(buffer[index - 1]);
                index--;
            }
            var partial = new string(stack.ToArray());
            completionCandidates = _completionEngine.GetCompletions(partial);
            if (completionCandidates == null || completionCandidates.Length == 0)
            {
                completionCandidates = null;
            }
            else
            {
                var completion = completionCandidates[completionIndex];
                buffer.Insert(bufferIndex, completion);
                selection.Reset(bufferIndex);

                bufferIndex += completion.Length;
                selection.Resize(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }

            return completionCandidates;
        }

        private static int Insert(string value, int startLeft, int startTop, StringBuilder buffer, Selection selection, int bufferIndex)
        {
            if (selection.Length > 0)
            {
                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(new string(' ', buffer.Length));

                buffer.Remove(selection.Start, selection.Length);
                bufferIndex = selection.Start;

                buffer.Insert(bufferIndex, value);

                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(buffer.ToString());

                bufferIndex += value.Length;
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }
            else
            {
                buffer.Insert(bufferIndex, value);

                bufferIndex += value.Length;
                Console.Write(value);
                Console.Write(buffer.ToString(bufferIndex, buffer.Length - bufferIndex));
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }

            return bufferIndex;
        }

        private static int Delete(int startLeft, int startTop, StringBuilder buffer, Selection selection, int bufferIndex, ConsoleKeyInfo keyInfo)
        {
            if (selection.Length > 0)
            {
                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(new string(' ', buffer.Length));

                buffer.Remove(selection.Start, selection.Length);
                bufferIndex = selection.Start;

                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(buffer.ToString());
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }
            else
            {
                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(new string(' ', buffer.Length));
                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                {
                    var sizeToDelete = GetNextWordBufferIndex(buffer, bufferIndex) - bufferIndex;
                    for (int i = 0; i < sizeToDelete; i++)
                    {
                        buffer.Remove(bufferIndex, 1);
                    }
                }
                else buffer.Remove(bufferIndex, 1);
                Console.SetCursorPosition(startLeft, startTop);
                Console.Write(buffer.ToString());
                SetCursorPosition(bufferIndex, startLeft, startTop);
                selection.Reset(bufferIndex);
            }

            return bufferIndex;
        }

        private static int GetPreviousWordBufferIndex(StringBuilder buffer, int bufferIndex)
        {
            // If spaces are to the left, go past them first.
            while (bufferIndex > 0 && buffer[bufferIndex - 1] == ' ')
            {
                bufferIndex = Math.Max(bufferIndex - 2, 0);
            }

            // Go left past any non-spaces until the char to the left is a space.
            for (bufferIndex--; bufferIndex >= 0; bufferIndex--)
            {
                if (buffer[bufferIndex] == ' ')
                {
                    bufferIndex++;
                    break;
                }
            }

            return Math.Max(bufferIndex, 0);
        }

        private static int GetNextWordBufferIndex(StringBuilder buffer, int bufferIndex)
        {
            // If there are any non-spaces to the right, go past them first.
            while (bufferIndex < buffer.Length - 1 && buffer[bufferIndex + 1] != ' ')
            {
                bufferIndex = Math.Min(bufferIndex + 1, buffer.Length);
            }

            // Go right past any spaces until the char to the right is a non-space.
            for (bufferIndex++; bufferIndex < buffer.Length; bufferIndex++)
            {
                if (buffer[bufferIndex] != ' ')
                {
                    break;
                }
            }

            return bufferIndex;
        }

        private int GetBufferIndexFromCursor(int startLeft, int startTop)
        {
            var left = startLeft;
            var top = startTop;
            int bufferIndex;
            var sanityCheck = Console.WindowWidth * Console.WindowHeight;
            for (bufferIndex = 0;
                 (left != Console.CursorLeft || top != Console.CursorTop)
                    && bufferIndex <= sanityCheck;
                bufferIndex++)
            {
                left++;
                if (left >= Console.WindowWidth)
                {
                    left = 0;
                    top++;
                }
            }
            return bufferIndex;
        }

        private static void SetCursorPosition(int bufferIndex, int startLeft, int startTop)
        {
            int left, top;
            GetCursorPosition(bufferIndex, startLeft, startTop, out left, out top);
            Console.SetCursorPosition(left, top);
        }

        private static void GetCursorPosition(int bufferIndex, int startLeft, int startTop, out int left, out int top)
        {
            left = startLeft;
            top = startTop;

            for (int i = 0; i < bufferIndex; i++)
            {
                left++;
                if (left >= Console.WindowWidth)
                {
                    left = 0;
                    top++;
                }
            }
        }

        private class Selection
        {
            private readonly StringBuilder _buffer;
            private readonly int _startLeft;
            private readonly int _startTop;

            public Selection(StringBuilder buffer, int startLeft, int startTop)
            {
                _buffer = buffer;
                _startLeft = startLeft;
                _startTop = startTop;
            }

            public void Reset(int bufferIndex)
            {
                if (Beginning != End)
                {
                    Console.SetCursorPosition(_startLeft, _startTop);
                    Console.Write(_buffer.ToString());
                    SetCursorPosition(End, _startLeft, _startTop);
                }
                Beginning = bufferIndex;
                End = Beginning;
            }

            public void Resize(int bufferIndex)
            {
                // Clear any old formatting.
                Console.SetCursorPosition(_startLeft, _startTop);
                Console.Write(_buffer.ToString());

                End = bufferIndex;
                var index = IsExpandingToRight(bufferIndex) ? Beginning : bufferIndex;
                SetCursorPosition(index, _startLeft, _startTop);

                // Swap the console's foreground and background colors.
                ConsoleColor originalForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = Console.BackgroundColor;
                Console.BackgroundColor = originalForegroundColor;

                // Write this selection's portion of the buffer using the swapped colors.
                Console.Write(_buffer.ToString(Start, Length));

                // Swap the colors back to their original colors.
                Console.BackgroundColor = Console.ForegroundColor;
                Console.ForegroundColor = originalForegroundColor;

                SetCursorPosition(bufferIndex, _startLeft, _startTop);
            }

            public int Beginning { get; set; }
            public int End { get; set; }
            public int Start => Beginning < End ? Beginning : End;
            public int Length => Math.Abs(End - Beginning);
            public bool IsExpandingToRight(int bufferIndex) => (Beginning == End && bufferIndex > End) || Beginning < End;
        }

        private static class Clipboard
        {
            public static string Get()
            {
                try
                {
                    if (!NativeMethods.OpenClipboard(IntPtr.Zero)) return null;
                    var clipboardPtr = NativeMethods.GetClipboardData(13);
                    if (clipboardPtr == IntPtr.Zero) return null;
                    IntPtr lockPtr = IntPtr.Zero;
                    try
                    {
                        lockPtr = NativeMethods.GlobalLock(clipboardPtr);
                        if (lockPtr == IntPtr.Zero) return null;
                        var size = NativeMethods.GlobalSize(clipboardPtr);
                        var buffer = new byte[size];
                        Marshal.Copy(lockPtr, buffer, 0, size);
                        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                    }
                    finally
                    {
                        if (lockPtr != IntPtr.Zero) NativeMethods.GlobalUnlock(clipboardPtr);
                    }
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            public static void Set(string value)
            {
                if (!NativeMethods.OpenClipboard(IntPtr.Zero)) return;
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    if (!NativeMethods.EmptyClipboard()) return;
                    ptr = Marshal.StringToHGlobalUni(value);
                    if (!NativeMethods.SetClipboardData(13, ptr)) return;
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            private static class NativeMethods
            {
                [DllImport("user32.dll")]
                public static extern bool OpenClipboard(IntPtr hWndNewOwner);

                [DllImport("user32.dll")]
                public static extern bool CloseClipboard();

                [DllImport("user32.dll")]
                public static extern bool EmptyClipboard();

                [DllImport("user32.dll")]
                public static extern bool SetClipboardData(uint uFormat, IntPtr data);

                [DllImport("user32.dll")]
                public static extern IntPtr GetClipboardData(uint uFormat);

                [DllImport("kernel32.dll")]
                public static extern int GlobalSize(IntPtr hMem);

                [DllImport("kernel32.dll")]
                public static extern IntPtr GlobalLock(IntPtr hMem);

                [DllImport("kernel32.dll")]
                public static extern bool GlobalUnlock(IntPtr hMem);
            }
        }
    }
}
