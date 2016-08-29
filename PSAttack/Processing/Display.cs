using System;
using PSAttack.Shell;

namespace PSAttack.Processing
{
    internal class Display : IDisplay
    {
        internal Display(CommandProcessor processor)
        {
            _processor = processor;
            Prompt = _processor.CurrentPath + PromptSuffix;
            DisplayedCommandInsertionIndex = Prompt.Length;
            DisplayedCommand = string.Empty;
            return;
        }

        internal int ConsoleWrapCount
        {
            get { return TotalDisplayLength / Console.WindowWidth; }
        }

        public int DisplayedCommandInsertionIndex { get; private set; }

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
            get { return DisplayedCommandInsertionIndex - Prompt.Length; }
        }

        // return cursor pos ignoring window wrapping
        public int RelativeCursorPosition
        {
            get
            {
                int wrapCount = this.ConsoleWrapCount;
                return DisplayedCommandInsertionIndex + ((0 < wrapCount) ? (Console.WindowWidth * wrapCount) : 0);
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

        /// <summary>This is used to figure out where the cursor should be
        /// placed, accounting for line wraps in the command and where the
        /// prompt is.</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        internal void GetCursorPosition(out int x, out int y)
        {
            int windowWidth = Console.WindowWidth;

            // figure out if we've dropped down a line
            int additionalLinesCount = DisplayedCommandInsertionIndex / windowWidth;
            int cursorY = _promptLineNumber + additionalLinesCount;
            int cursorX = DisplayedCommandInsertionIndex - (windowWidth * additionalLinesCount);

            // if X is < 0, set cursor to end of line
            x = (0 > cursorX) ? windowWidth - 1 : cursorX;
            y = cursorY;
            return;
        }

        /// <summary>Setup cursor just after prompt.</summary>
        internal void HomeCursor()
        {
            DisplayedCommandInsertionIndex = Prompt.Length;
        }

        internal void InsertCommandCharacter(char candidate)
        {
            // figure out where to insert the typed character
            DisplayedCommand = DisplayedCommand.Insert((DisplayedCommandInsertionIndex++) - Prompt.Length,
                new string(candidate, 1));
            return;
        }

        internal void MoveCursor(bool forward)
        {
            DisplayedCommandInsertionIndex += (forward) ? 1 : -1;
        }

        internal void Output(bool commandCompleted)
        {
            if (commandCompleted) { PrintPrompt(); }
            // This is where we juggle things to make sure the cursor ends up where 
            // it's expected to be. I'm sure this could be improved on.

            // Clear out typed text after prompt.
            int promptLength = Prompt.Length;
            int windowWidth = Console.WindowWidth;
            char[] whiteLine = new string(' ', windowWidth).ToCharArray();
            Console.SetCursorPosition(promptLength, _promptLineNumber);
            Console.Write(whiteLine, 0, windowWidth - promptLength);

            // Clear out any lines below the prompt
            for (int additionalLineNumber = ConsoleWrapCount; 0 < additionalLineNumber; additionalLineNumber--) {
                Console.SetCursorPosition(0, _promptLineNumber + additionalLineNumber);
                Console.Write(whiteLine);
            }
            Console.SetCursorPosition(promptLength, _promptLineNumber);

            // Re-print the command
            Console.Write(DisplayedCommand);
            int left;
            int top;
            this.GetCursorPosition(out left, out top);
            Console.SetCursorPosition(left, top);
        }

        internal void PrintPrompt()
        {
            _promptLineNumber = Console.CursorTop;
            Write(PSColors.prompt, Prompt);
            DisplayedCommandInsertionIndex = Prompt.Length;
            return;
        }

        internal void Reset()
        {
            DisplayedCommand = string.Empty;
        }

        internal void SetCursorAfterCommand()
        {
            DisplayedCommandInsertionIndex = Prompt.Length + DisplayedCommand.Length;
        }

        internal void SetCursorAfterDisplayedCommand()
        {
            DisplayedCommandInsertionIndex = EndOfDisplayCmdPos;
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
        /// <summary>The vertical position of the last prompt printed. Used so
        /// we know where to start re-writing commands. The topmost line is
        /// numbered 0.</summary>
        private int _promptLineNumber;
    }
}
