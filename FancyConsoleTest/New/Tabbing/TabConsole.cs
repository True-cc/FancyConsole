using FancyConsoleTest.New.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FancyConsoleTest.New.Minimal
{
    public class TabConsole
    {
        private static readonly char[] Whitespace = new[] {' ', '\t', '\n', '\u200b', '|'};
        public static readonly TabConsole Instance = new TabConsole();
        private readonly List<string> _history = new List<string>();
        private int _historyIndex;
        private string _savedLine = "";
        private bool _hinting;
        private List<string> _hints = new List<string>();
        private int _hintIndex;
        private string _currentArg = "";
        private string _line = "";
        private int _cursor;
        private ConsoleKeyInfo _key;

        public void StartInputting(string[] args)
        {
            while (true)
            {
                _key = Console.ReadKey(true);
                DoKeyStuff();
            }
        }

        private void DoKeyStuff()
        {
            switch (_key.Key)
            {
                case ConsoleKey.End:
                    GuiApp.Log(new FancyText($"UwU{new Random().Next()}", FancyColor.Aqua));
                    break;
                case ConsoleKey.Tab:
                    int rem;
                    if (_hinting)
                    {
                        _hintIndex++;
                        if (_hintIndex >= _hints.Count) _hintIndex = 0;
                        rem = GetSkipAmount(-1, true);
                    }
                    else
                    {
                        _hinting = true;
                        var hints = GetTabbyThingy.GetHints(_line);
                        _hints = hints.Where(s => s.StartsWith(_currentArg)).ToList();
                        _hintIndex = 0;
                        rem = _currentArg.Length;
                    }

                    if (_hints.Count == 0) return;

                    if (rem > 0) _line = _line.Remove(_cursor - rem, rem);

                    AddCursorPos(-rem);

                    var hint = _hints[_hintIndex];
                    _line = _line.Insert(_cursor, hint);
                    AddCursorPos(hint.Length);

                    SetCursorPos();
                    return;
                case ConsoleKey.Enter:
                    Console.Write("\n");
                    _history.Insert(0, _line);
                    GuiApp.LineRed(_line);
                    Reset();
                    break;
                case ConsoleKey.LeftArrow:
                    AddCursorPos(_key.Modifiers.HasFlag(ConsoleModifiers.Control) ? -GetSkipAmount(-1, true) : -1);
                    break;
                case ConsoleKey.RightArrow:
                    AddCursorPos(_key.Modifiers.HasFlag(ConsoleModifiers.Control) ? GetSkipAmount(1, false) : 1);
                    break;
                case ConsoleKey.UpArrow:
                    History(1);
                    break;
                case ConsoleKey.DownArrow:
                    History(-1);
                    break;
                case ConsoleKey.Backspace:
                    if (_key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        var amount = GetSkipAmount(-1, true);
                        if (amount > 0) _line = _line.Remove(_cursor - amount, amount);
                        AddCursorPos(-amount);
                    }
                    else
                    {
                        if (_cursor > 0) _line = _line.Remove(_cursor - 1, 1);
                        AddCursorPos(-1);
                    }

                    break;
                case ConsoleKey.Delete:
                    if (_key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        var amount = GetSkipAmount(1, false);
                        if (amount > 0 && _cursor < _line.Length) _line = _line.Remove(_cursor, amount);
                    }
                    else
                    {
                        if (_cursor < _line.Length) _line = _line.Remove(_cursor, 1);
                    }

                    break;
                default:
                    HandleKey();
                    break;
            }

            _hinting = false;
            SetCurrentArg();
            SetCursorPos();
        }

        private void HandleKey()
        {
            if (_key.KeyChar <= 31) return;
            _line = _line.Insert(_cursor, _key.KeyChar.ToString());
            AddCursorPos(1);
        }

        private void Reset()
        {
            _hinting = false;
            _currentArg = _line = _savedLine = "";
            _cursor = _hintIndex = 0;
            _historyIndex = -1;
        }

        private void SetCurrentArg()
        {
            var s = _line.Substring(0, _cursor);
            var i = s.LastIndexOf(' ') + 1;
            _currentArg = s.Substring(i);
        }

        private void SetCursorPos()
        {
            if (ConsoleUtils.Width < 5) return;
            Console.CursorVisible = false;
            AddCursorPos();
            Console.SetCursorPosition(0, Console.CursorTop);
            var i = Math.Max(0, _cursor - ConsoleUtils.Width + 3);
            var printLine = _line.Substring(i,
                Math.Min(_line.Length, _cursor < ConsoleUtils.Width ? _line.Length : _cursor + 2) - i);
            Console.Write(printLine + new string(' ', Math.Max(0, ConsoleUtils.Width - printLine.Length - 1)));
            Console.SetCursorPosition(Math.Min(ConsoleUtils.Width - 3, _cursor), Console.CursorTop);
            Console.CursorVisible = true;
        }

        private void AddCursorPos(int i = 0)
        {
            _cursor += i;
            if (_cursor < 0) _cursor = 0;
            else if (_cursor > _line.Length) _cursor = _line.Length;
        }

        private int GetSkipAmount(int dir, bool skipLastWhitespace)
        {
            var skipped = 0;
            var index = _cursor;
            var inWhitespace = _cursor + dir > _line.Length || _cursor <= 0 ||
                               Whitespace.Contains(_line[_cursor + dir]);
            while ((index += dir) >= 0 && (index < _line.Length))
            {
                skipped++;
                if (inWhitespace) inWhitespace = Whitespace.Contains(_line[index]);
                if (inWhitespace || !Whitespace.Contains(_line[index])) continue;
                if (skipLastWhitespace) skipped--;
                break;
            }

            if (index >= _line.Length) skipped++;
            return skipped;
        }

        private void History(int i)
        {
            if (_historyIndex < 0) _savedLine = _line;
            _historyIndex += i;
            if (_historyIndex < -1) _historyIndex = -1;
            else if (_historyIndex >= _history.Count) _historyIndex = _history.Count - 1;
            _line = _historyIndex < 0 ? _savedLine : _history[_historyIndex];
            _cursor = _line.Length;
        }

        public void Log(FancyText text)
        {
            Console.CursorVisible = false;
            var top = Math.Max(0, Console.CursorTop);
            Console.SetCursorPosition(0, top);
            Console.Write(new string(' ', ConsoleUtils.Width - 1));
            Console.SetCursorPosition(0, top);
            text.SetNext(new FancyText("\n", FancyColor.Reset));
            text.PrintNext(GuiApp.ConsoleColors);

            Console.SetCursorPosition(0, Math.Max(0, top + text.GetLines(ConsoleUtils.Width).Count));
            SetCursorPos();
        }
    }
}