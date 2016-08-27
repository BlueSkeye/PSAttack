using System;
using PSAttack.Shell;

namespace PSAttack.Processing
{
    internal class Display
    {
        internal Display(CommandProcessor processor)
        {
            _processor = processor;
        }

        private string CreatePrompt(AttackState attackState)
        {
            string prompt = attackState.runspace.SessionStateProxy.Path.CurrentLocation + PromptSuffix;
            attackState.promptLength = prompt.Length;
            return prompt;
        }

        internal void Exception(string errorMsg)
        {
            Console.ForegroundColor = PSColors.errorText;
            Console.WriteLine("ERROR: {0}\n", errorMsg);
        }

        public void Output(AttackState attackState)
        {
            if (attackState.IsCommandComplete) { PrintPrompt(attackState); }
            int currentCusorPos = Console.CursorTop;
            string prompt = CreatePrompt(attackState);

            // This is where we juggle things to make sure the cursor ends up where 
            // it's expected to be. I'm sure this could be improved on.

            // Clear out typed text after prompt
            Console.SetCursorPosition(prompt.Length, attackState.promptPos);
            Console.Write(new string(' ', Console.WindowWidth));

            // Clear out any lines below the prompt
            int cursorDiff = attackState.ConsoleWrapCount;
            while (0 < cursorDiff) {
                Console.SetCursorPosition(0, attackState.promptPos + cursorDiff);
                Console.Write(new string(' ', Console.WindowWidth));
                cursorDiff -= 1;
            }
            Console.SetCursorPosition(prompt.Length, attackState.promptPos);

            // Re-print the command
            Console.Write(attackState.DisplayedCommand);
            int left;
            int top;
            attackState.GetCursorPosition(out left, out top);
            Console.SetCursorPosition(left, top);
        }

        internal void PrintPrompt(AttackState attackState)
        {
            attackState.promptPos = Console.CursorTop;
            string prompt = CreatePrompt(attackState);
            Write(PSColors.prompt, prompt);
            attackState.cursorPos = prompt.Length;
            return;
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
        private CommandProcessor _processor;
    }
}
