using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSAttack.Processing
{
    // This item is used to keep track of the various components on the command line
    public class DisplayCmdComponent
    {
        public string Contents { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
    }

    internal class AttackState
    {
        public AttackState()
        {
            Runspace runspace = RunspaceFactory.CreateRunspace(new PSAttackHost());
            runspace.Open();
            this.runspace = runspace;
            this.history = new List<string>();
            this.decryptedStore = new Dictionary<string, string>();
            // hack to keep cmd from being null. others parts of psa don't appreciate that.
            this.Command = string.Empty;
            this.DisplayedCommand = string.Empty;
            this.cursorPos = this.promptLength;
        }

        internal string Command { get; set; }

        internal int ConsoleWrapCount
        {
            get { return TotalDisplayLength / Console.WindowWidth; }
        }

        // used to store list of command components and their types
        public List<DisplayCmdComponent> cmdComponents { get; set; }
        // used to store index of command compnotent being auto-completed.
        public int cmdComponentsIndex { get; set; }
        // absolute cusor position (not accounting for wrapping in the window)
        public int cursorPos { get; set; }
        // When PSAttack is built an encrypted CSV is generated containing data that we 
        // don't want to touch disk. That data is stored here as a dict 
        public Dictionary<string, string> decryptedStore { get; set; }

        public string DisplayedCommand { get; set; }

        // string to store displayCmd for autocomplete concatenation
        public string displayCmdSeed { get; set; }
        // used to store command history
        public List<string> history { get; set; }

        internal bool IsCommandComplete { get; private set; }

        // key that was last pressed
        public ConsoleKeyInfo keyInfo { get; set; }
        // we set a loopPos for when we're in a tab-complete loop
        public int loopPos { get; set; }
        // loop states
        public string loopType { get; set; }
        // ouput is what's print to screen
        public string output { get; set; }
        public int promptLength { get; set; }
        // The vertical position of the last prompt printed. Used so we know where to start re-writing commands
        public int promptPos { get; set; }

        // return cursor pos ignoring window wrapping
        public int RelativeCursorPosition
        {
            get
            {
                int wrapCount = this.ConsoleWrapCount;
                return (0 < wrapCount)
                    ? this.cursorPos + Console.WindowWidth * wrapCount
                    : this.cursorPos;
            }
        }

        // used for auto-complete loops
        public Collection<PSObject> results { get; set; }
        // returns total length of display cmd + prompt. Used to check for text wrap in 
        // so we know what to do with our cursor
        // Powershell runspace and host
        public Runspace runspace { get; set; }

        private int TotalDisplayLength
        {
            get { return this.promptLength + this.DisplayedCommand.Length; }
        }

        // clear out cruft from working with commands
        public void ClearIO(bool display = false)
        {
            if (display) { this.DisplayedCommand = ""; }
            this.Command = "";
            this.keyInfo = new ConsoleKeyInfo();
            this.IsCommandComplete = false;
            this.output = null;
        }

        // clear out cruft from autocomplete loops
        public void ClearLoop()
        {
            this.loopType = null;
            this.results = null;
            this.displayCmdSeed = null;
            this.loopPos = 0;
            this.cmdComponents = null;
            this.cmdComponentsIndex = -1;
        }

        // return end of displayCmd accounting for prompt
        public int endOfDisplayCmdPos()
        {
            return this.promptLength + this.DisplayedCommand.Length;
        }

        // This is used to figure out where the cursor should be placed, accounting for line
        // wraps in the command and where the prompt is
        internal void GetCursorPosition(out int x, out int y)
        {
            // figure out if we've dropped down a line
            int cursorYDiff = this.cursorPos / Console.WindowWidth;
            int cursorY = this.promptPos + this.cursorPos / Console.WindowWidth;
            int cursorX = this.cursorPos - (Console.WindowWidth * cursorYDiff);

            // if X is < 0, set cursor to end of line
            x = (0 > cursorX) ? Console.WindowWidth - 1 : cursorX;
            y = cursorY;
        }

        // return relative cusor pos without prompt
        public int relativeCmdCursorPos()
        {
            return this.cursorPos - this.promptLength;
        }

        internal void SetCommandComplete()
        {
            IsCommandComplete = true;
        }
    }
}
