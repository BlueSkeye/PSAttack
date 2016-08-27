using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSAttack.Processing
{
    internal interface IDisplay
    {
        int CursorPosition { get; }
        string DisplayedCommand { get; }
        int PromptLength { get; }

        void SetDisplayedCommand(string command, int? cursorPosition = null);
    }
}
