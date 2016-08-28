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
        string Prompt { get; }

        void SetDisplayedCommand(string command);
    }
}
