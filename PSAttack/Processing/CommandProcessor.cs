using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using PSAttack.Shell;
using PSAttack.Utils;

namespace PSAttack.Processing
{
    internal class CommandProcessor
    {
        internal CommandProcessor()
        {
            _state = new AttackState();
            _tabExpander = new TabExpander(this);
            _display = new Display(this);
            Initialize();
        }
        
        internal void ProcessKey(ConsoleKeyInfo keyInfo)
        {
            try {
                _state.keyInfo = keyInfo;
                _state.output = null;
                int relativePos = _state.RelativeCursorPosition;
                int cmdLength = _state.DisplayedCommand.Length;
                List<char> displayCmd;
                switch (_state.keyInfo.Key) {
                    case ConsoleKey.Backspace:
                    case ConsoleKey.Delete:
                        _state.ClearLoop();
                        if (_state.DisplayedCommand != "" && _state.RelativeCursorPosition > 0) {
                            if (_state.keyInfo.Key == ConsoleKey.Backspace) {
                                _state.cursorPos -= 1;
                            }
                            displayCmd = _state.DisplayedCommand.ToList();
                            int relativeCursorPos = _state.relativeCmdCursorPos();
                            displayCmd.RemoveAt(relativeCursorPos);
                            _state.DisplayedCommand = new string(displayCmd.ToArray());
                        }
                        return;
                    case ConsoleKey.Home:
                        _state.cursorPos = _state.promptLength;
                        return;
                    case ConsoleKey.End:
                        _state.cursorPos = _state.promptLength + _state.DisplayedCommand.Length;
                        return;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                        HandleHistoryKey();
                        return;
                    case ConsoleKey.LeftArrow:
                        // TODO: Fix arrows navigating between wrapped command lines
                        if (_state.relativeCmdCursorPos() > 0) {
                            _state.ClearLoop();
                            _state.cursorPos -= 1;
                        }
                        return;
                    case ConsoleKey.RightArrow:
                        if (_state.relativeCmdCursorPos() < _state.DisplayedCommand.Length) {
                            _state.ClearLoop();
                            _state.cursorPos += 1;
                        }
                        return;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        _state.ClearLoop();
                        _state.Command = _state.DisplayedCommand;
                        // don't add blank lines to history
                        if (string.Empty != _state.Command) {
                            _state.history.Add(_state.Command);
                        }
                        if (_state.Command == "exit") {
                            Environment.Exit(0);
                        }
                        if (_state.Command == "clear") {
                            Console.Clear();
                            _state.DisplayedCommand = "";
                            _display.PrintPrompt(_state);
                        }
                        // TODO: Make this better.
                        else if (_state.Command.Contains(".exe")) {
                            ExecutePSCommand("Start-Process -NoNewWindow -Wait " + _state.Command);
                            _display.Output(_state);
                        }
                        // assume that we just want to execute whatever makes it here.
                        else {
                            ExecutePSCommand();
                            _state.DisplayedCommand = "";
                            _display.Output(_state);
                        }
                        // clear out cmd related stuff from state
                        _state.ClearIO(display: true);
                        return;
                    case ConsoleKey.Tab:
                        _tabExpander.Process(_state);
                        return;
                    default:
                        // if nothing matched, lets assume its a character and add it to displayCmd
                        _state.ClearLoop();
                        // figure out where to insert the typed character
                        displayCmd = _state.DisplayedCommand.ToList();
                        int relativeCmdCursorPos = _state.relativeCmdCursorPos();
                        int cmdInsertPos = _state.cursorPos - _state.promptLength;
                        displayCmd.Insert(_state.cursorPos - _state.promptLength, _state.keyInfo.KeyChar);
                        _state.DisplayedCommand = new string(displayCmd.ToArray());
                        _state.cursorPos += 1;
                        return;
                }
            }
            finally { _display.Output(_state); }
        }

        // called when up or down is entered
        private void HandleHistoryKey()
        {
            if (0 < _state.history.Count) {
                if (null == _state.loopType) {
                    _state.loopType = "history";
                    if (0 == _state.loopPos) {
                        _state.loopPos = _state.history.Count;
                    }
                }
                switch (_state.keyInfo.Key) {
                    case ConsoleKey.UpArrow:
                        if (0 < _state.loopPos) {
                            _state.loopPos -= 1;
                            _state.DisplayedCommand = _state.history[_state.loopPos];
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if ((_state.loopPos + 1) > (_state.history.Count - 1)) {
                            _state.DisplayedCommand = "";
                        }
                        else {
                            _state.loopPos += 1;
                            _state.DisplayedCommand = _state.history[_state.loopPos];
                        }
                        break;
                    default:
                        break;
                }
                _state.cursorPos = _state.endOfDisplayCmdPos();
            }
            return;
        }

        private void ImportModule(Stream moduleStream)
        {
            try {
                using (MemoryStream decMem = CryptoUtils.DecryptFile(moduleStream)) {
                    this.ExecutePSCommand(Encoding.Unicode.GetString(decMem.ToArray()));
                }
            }
            catch (Exception e) {
                _display.Write(ConsoleColor.Red, Strings.moduleLoadError, e.Message);
            }
        }

        private void Initialize()
        {
            // Display Loading Message
            Console.ForegroundColor = PSColors.logoText;
            Random random = new Random();
            int pspLogoInt = random.Next(Strings.psaLogos.Count);
            Console.WriteLine(Strings.psaLogos[pspLogoInt]);
            Console.WriteLine("PS>Attack is loading...");

            // Get Encrypted Values
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream valueStream = assembly.GetManifestResourceStream("PSAttack.Resources." + Properties.Settings.Default.valueStore);
            MemoryStream valueStore = CryptoUtils.DecryptFile(valueStream);
            string valueStoreStr = Encoding.Unicode.GetString(valueStore.ToArray());
            string[] valuePairs = valueStoreStr.Replace("\r","").Split('\n');

            foreach (string value in valuePairs) {
                if (string.IsNullOrEmpty(value)) { continue; }
                string[] entry = value.Split('|');
                _state.decryptedStore.Add(entry[0], entry[1]);
            }

            // amsi bypass (thanks matt!!)
            if (9 < Environment.OSVersion.Version.Major) {
                try { ExecutePSCommand(_state.decryptedStore["amsiBypass"]); }
                catch { Console.WriteLine("Could not run AMSI bypass."); }
            }

            // Decrypt modules
            foreach (string resource in assembly.GetManifestResourceNames()) {
                if (!resource.Contains(ModulePrefix)) { continue; }
                Console.ForegroundColor = PSColors.loadingText;
                Console.WriteLine("Decrypting: {0}",
                    CryptoUtils.DecryptString(resource.Replace(ModulePrefix, string.Empty)));
                this.ImportModule(assembly.GetManifestResourceStream(resource));
            }

            // Setup PS env
            ExecutePSCommand(_state.decryptedStore["setExecutionPolicy"]);

            // check for admin 
            bool isAdmin = false;
            bool debugProc = false;
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
                isAdmin = true;
                try {
                    System.Diagnostics.Process.EnterDebugMode();
                    debugProc = true;
                }
                catch { Console.Write("Could not grab debug rights for process."); }
            }
            
            // Setup Console
            Console.Title = Strings.windowTitle;
            Console.BufferHeight = Int16.MaxValue - 10;
            Console.BackgroundColor = PSColors.background;
            Console.TreatControlCAsInput = true;
            Console.Clear();

            // get build info
            string buildString;
            string attackDate = new StreamReader(assembly.GetManifestResourceStream("PSAttack.Resources.attackDate.txt")).ReadToEnd();
            Boolean builtWithBuildTool = true;
            if (12 < attackDate.Length) {                
                buildString = "It was custom made by the PS>Attack Build Tool on " + attackDate + "\n"; 
            }
            else {
                string buildDate = new StreamReader(assembly.GetManifestResourceStream("PSAttack.Resources.BuildDate.txt")).ReadToEnd();
                buildString = "It was built on " + buildDate + "\nIf you'd like a version of PS>Attack thats even harder for AV \nto detect checkout http://github.com/jaredhaight/PSAttackBuildTool \n";
                builtWithBuildTool = false;
            }

            // Figure out if we're 32 or 64bit
            string arch = (sizeof(uint) == IntPtr.Size) ? "32bit" : "64bit";

            // setup debug variable
            string debugCmd = "$debug = @{'psaVersion'='" + Strings.version + "';'osVersion'='" + Environment.OSVersion.ToString() + "';'.NET'='"
                + System.Environment.Version + "';'isAdmin'='"+ isAdmin + "';'builtWithBuildTool'='" + builtWithBuildTool.ToString() +"';'debugRights'='"
                + debugProc + "';'arch'='" + arch + "'}";
            ExecutePSCommand(debugCmd);

            // print intro
            Console.ForegroundColor = PSColors.introText;
            Console.WriteLine(Strings.welcomeMessage, Strings.version, buildString);

            // Display Prompt
            _state.ClearLoop();
            _state.ClearIO();
            _display.PrintPrompt(_state);
            return;
        }

        // Here is where we execute posh code
        internal void ExecutePSCommand(string command = null)
        {
            if (null != command) { _state.Command = command; }
            using (Pipeline pipeline = _state.runspace.CreatePipeline()) {
                pipeline.Commands.AddScript(_state.Command);
                // If we're in an auto-complete loop, we want the PSObjects, not the string from the output of the command
                // TODO: clean this up
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                if (null == _state.loopType) {
                    pipeline.Commands.Add("out-default");
                }
                try { _state.results = pipeline.Invoke(); }
                catch (Exception e) {
                    _state.results = null;
                    _display.Exception(e.Message);
                }
            }
            //Clear out command so it doesn't get echo'd out to console again.
            _state.ClearIO();
            if (null == _state.loopType) {
                _state.SetCommandComplete();
            }
            return;
        }

        private const string ModulePrefix = "PSAttack.Modules.";
        private Display _display;
        private AttackState _state;
        private TabExpander _tabExpander;
    }
}