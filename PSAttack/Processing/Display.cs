﻿using System;
using PSAttack.Shell;

namespace PSAttack.Processing
{
    internal class Display : IDisplay
    {
        internal Display(CommandProcessor processor)
        {
            _processor = processor;
            Prompt = _processor.CurrentPath + PromptSuffix;
            CursorPosition = Prompt.Length;
            DisplayedCommand = string.Empty;
            return;
        }

        internal int ConsoleWrapCount
        {
            get { return TotalDisplayLength / Console.WindowWidth; }
        }

        public int CursorPosition { get; private set; }

        public string DisplayedCommand { get; private set; }

        // return end of displayCmd accounting for prompt
        public int EndOfDisplayCmdPos
        {
            get { return Prompt.Length + DisplayedCommand.Length; }
        }

        public string Prompt { get; private set; }

        // return relative cusor pos without prompt
        public int RelativeCmdCursorPos
        {
            get { return _cursorPos - Prompt.Length; }
        }

        // return cursor pos ignoring window wrapping
        public int RelativeCursorPosition
        {
            get
            {
                int wrapCount = this.ConsoleWrapCount;
                return (0 < wrapCount)
                    ? CursorPosition + Console.WindowWidth * wrapCount
                    : CursorPosition;
            }
        }

        private int TotalDisplayLength
        {
            get { return Prompt.Length + DisplayedCommand.Length; }
        }

        internal void DisplayException(string errorMsg)
        {
            Console.ForegroundColor = PSColors.errorText;
            Console.WriteLine("ERROR: {0}\n", errorMsg);
        }

        // This is used to figure out where the cursor should be placed, accounting for line
        // wraps in the command and where the prompt is
        internal void GetCursorPosition(out int x, out int y)
        {
            // figure out if we've dropped down a line
            int cursorYDiff = CursorPosition / Console.WindowWidth;
            int cursorY = _promptPos + CursorPosition / Console.WindowWidth;
            int cursorX = CursorPosition - (Console.WindowWidth * cursorYDiff);

            // if X is < 0, set cursor to end of line
            x = (0 > cursorX) ? Console.WindowWidth - 1 : cursorX;
            y = cursorY;
        }

        internal void HomeCursor()
        {
            CursorPosition = Prompt.Length;
        }

        internal void InsertCommandCharacter(char candidate)
        {
            // figure out where to insert the typed character
            this.DisplayedCommand = DisplayedCommand.Insert(CursorPosition - Prompt.Length, new string(candidate, 1));
            return;
        }

        internal void MoveCursor(bool forward)
        {
            CursorPosition += (forward) ? 1 : -1;
        }

        internal void Output(bool commandCompleted)
        {
            if (commandCompleted) { PrintPrompt(); }
            // This is where we juggle things to make sure the cursor ends up where 
            // it's expected to be. I'm sure this could be improved on.
            // Clear out typed text after prompt
            Console.SetCursorPosition(Prompt.Length, _promptPos);
            int windowWidth = Console.WindowWidth;
            Console.Write(new string(' ', windowWidth));

            // Clear out any lines below the prompt
            for (int cursorDiff = ConsoleWrapCount; 0 < cursorDiff; cursorDiff--) {
                Console.SetCursorPosition(0, _promptPos + cursorDiff);
                Console.Write(new string(' ', windowWidth));
            }
            Console.SetCursorPosition(Prompt.Length, _promptPos);

            // Re-print the command
            Console.Write(DisplayedCommand);
            int left;
            int top;
            this.GetCursorPosition(out left, out top);
            Console.SetCursorPosition(left, top);
        }

        internal void PrintPrompt()
        {
            _promptPos = Console.CursorTop;
            Write(PSColors.prompt, Prompt);
            CursorPosition = Prompt.Length;
            return;
        }

        internal void Reset()
        {
            DisplayedCommand = string.Empty;
        }

        internal void SetCursorAfterCommand()
        {
            CursorPosition = Prompt.Length + DisplayedCommand.Length;
        }

        internal void SetCursorAfterDisplayedCommand()
        {
            CursorPosition = EndOfDisplayCmdPos;
        }

        public void SetDisplayedCommand(string displayed)
        {
            DisplayedCommand = displayed;
        }

        internal void Write(ConsoleColor color, string message, params object[] args)
        {
            ConsoleColor initialColor = Console.ForegroundColor;
            try {
                Console.ForegroundColor = color;
                Console.Write(message, args);
            }
            finally { Console.ForegroundColor = initialColor; }
        }

        private const string PromptSuffix = " #> ";
        // absolute cusor position (not accounting for wrapping in the window)
        private int _cursorPos;
        private CommandProcessor _processor;
        // The vertical position of the last prompt printed. Used so we know where to start re-writing commands
        private int _promptPos;
    }
}