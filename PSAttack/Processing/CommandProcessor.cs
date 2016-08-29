using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
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
            _runspace = RunspaceFactory.CreateRunspace(new PSAttackHost());
            _runspace.Open();
            _decryptedStore = new Dictionary<string, string>();
            _display = new Display(this);
            _tabExpander = new TabExpander(this, _display);
            CurrentPath = _runspace.SessionStateProxy.Path.CurrentLocation;
            Initialize();
            return;
        }

        internal string Command { get; private set; }

        internal PathInfo CurrentPath { get; private set; }

        internal PSObject CurrentResult
        {
            get { return _results[_commandHistoryPosition]; }
        }

        internal bool HasResults
        {
            get { return (0 < this._results.Count); }
        }

        private bool IsInLoop
        {
            get { return (CommandItemType.Undefined != _loopType); }
        }

        // called when up or down is entered
        private void HandleHistoryKey(ConsoleKeyInfo keyInfo)
        {
            if (0 < _commandHistory.Count) {
                if (!IsInLoop) {
                    _loopType = CommandItemType.History;
                    if (0 == _commandHistoryPosition) {
                        _commandHistoryPosition = _commandHistory.Count;
                    }
                }
                switch (keyInfo.Key) {
                    case ConsoleKey.UpArrow:
                        if (0 < _commandHistoryPosition) {
                            _display.SetDisplayedCommand(_commandHistory[--_commandHistoryPosition]);
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if ((_commandHistoryPosition + 1) > (_commandHistory.Count - 1)) {
                            _display.SetDisplayedCommand(string.Empty);
                        }
                        else {
                            _display.SetDisplayedCommand(_commandHistory[++_commandHistoryPosition]);
                        }
                        break;
                    default:
                        break;
                }
                _display.SetCursorAfterDisplayedCommand();
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
                _decryptedStore.Add(entry[0], entry[1]);
            }

            // amsi bypass (thanks matt!!)
            if (9 < Environment.OSVersion.Version.Major) {
                try { ExecutePSCommand(_decryptedStore["amsiBypass"]); }
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
            ExecutePSCommand(_decryptedStore["setExecutionPolicy"]);

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
            bool builtWithBuildTool = true;
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
            ResetLooping();
            ResetCommand();
            _display.PrintPrompt();
            return;
        }

        // Here is where we execute posh code
        internal void ExecutePSCommand(string command = null)
        {
            if (null != command) { Command = command; }
            using (Pipeline pipeline = _runspace.CreatePipeline()) {
                pipeline.Commands.AddScript(Command);
                // If we're in an auto-complete loop, we want the PSObjects, not the string from the output of the command
                // TODO: clean this up
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                if (!IsInLoop) {
                    pipeline.Commands.Add("out-default");
                }
                try { _results = pipeline.Invoke(); }
                catch (Exception e) {
                    _results = null;
                    _display.DisplayException(e.Message);
                }
            }
            // Clear out command so it doesn't get echo'd out to console again.
            ResetCommand();
            if (!IsInLoop) {
                _commandCompleted = true;
            }
            return;
        }

        internal void MoveCurrentHistoryPosition(bool backward)
        {
            if (backward) {
                _commandHistoryPosition -= 1;
                // loop around if we're at the beginning
                if (0 > _commandHistoryPosition) {
                    _commandHistoryPosition = _results.Count - 1;
                }
            }
            else {
                _commandHistoryPosition += 1;
                // loop around if we reach the end
                if (_results.Count <= _commandHistoryPosition) {
                    _commandHistoryPosition = 0;
                }
            }
        }
        
        internal void ProcessKey(ConsoleKeyInfo keyInfo)
        {
            try {
                int relativePos = _display.RelativeCursorPosition;
                int cmdLength = _display.DisplayedCommand.Length;
                List<char> displayCmd;
                switch (keyInfo.Key) {
                    case ConsoleKey.Backspace:
                    case ConsoleKey.Delete:
                        ResetLooping();
                        if (   (string.Empty!= _display.DisplayedCommand)
                            && (0 < _display.RelativeCursorPosition))
                        {
                            if (ConsoleKey.Backspace == keyInfo.Key) {
                                _display.MoveCursor(false);
                            }
                            displayCmd = new List<char>(_display.DisplayedCommand);
                            int relativeCursorPos = _display.RelativeCmdCursorPos;
                            displayCmd.RemoveAt(relativeCursorPos);
                            _display.SetDisplayedCommand(new string(displayCmd.ToArray()));
                        }
                        return;
                    case ConsoleKey.Home:
                        _display.HomeCursor();
                        return;
                    case ConsoleKey.End:
                        _display.SetCursorAfterCommand();
                        return;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                        HandleHistoryKey(keyInfo);
                        return;
                    case ConsoleKey.LeftArrow:
                        // TODO: Fix arrows navigating between wrapped command lines
                        if (0 < _display.RelativeCmdCursorPos) {
                            ResetLooping();
                            _display.MoveCursor(false);
                        }
                        return;
                    case ConsoleKey.RightArrow:
                        if (_display.RelativeCmdCursorPos < _display.DisplayedCommand.Length) {
                            ResetLooping();
                            _display.MoveCursor(true);
                        }
                        return;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        ResetLooping();
                        Command = _display.DisplayedCommand;
                        // don't add blank lines to history
                        if (string.Empty != Command) {
                            _commandHistory.Add(Command);
                        }
                        switch (Command) {
                            case "exit":
                                Environment.Exit(0);
                                break; // Never reached.
                            case "clear":
                                Console.Clear();
                                _display.SetDisplayedCommand(string.Empty);
                                _display.PrintPrompt();
                                break;
                            default:
                                if (Command.Contains(".exe")) {
                                    // assume that we just want to execute whatever makes it here.
                                    ExecutePSCommand("Start-Process -NoNewWindow -Wait " + Command);
                                }
                                else {
                                    ExecutePSCommand();
                                    _display.SetDisplayedCommand(string.Empty);
                                }
                                _display.Output(_commandCompleted);
                                break;
                        }
                        // clear out cmd related stuff from state
                        ResetCommand();
                        _display.Reset();
                        return;
                    case ConsoleKey.Tab:
                        _tabExpander.Process(keyInfo, ref _loopType);
                        return;
                    default:
                        // if nothing matched, lets assume its a character and add it to displayCmd
                        ResetLooping();
                        _display.InsertCommandCharacter(keyInfo.KeyChar);
                        return;
                }
            }
            finally { _display.Output(_commandCompleted); }
        }

        private void ResetCommand()
        {
            Command = string.Empty;
            _commandCompleted = false;
            return;
        }

        private void ResetLooping()
        {
            _commandHistoryPosition = 0;
            _results = null;
            _loopType = CommandItemType.Undefined;
            _tabExpander.Reset();
        }

        private const string ModulePrefix = "PSAttack.Modules.";
        private bool _commandCompleted;
        private List<string> _commandHistory = new List<string>();
        private int _commandHistoryPosition;
        // When PSAttack is built an encrypted CSV is generated containing data that we 
        // don't want to touch disk. That data is stored here as a dict 
        private Dictionary<string, string> _decryptedStore;
        private Display _display;
        private CommandItemType _loopType;
        private Collection<PSObject> _results { get; set; }
        private Runspace _runspace;
        private TabExpander _tabExpander;
    }
}