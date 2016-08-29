using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace PSAttack.Processing
{
    internal class TabExpander
    {
        internal TabExpander(CommandProcessor processor, IDisplay display)
        {
            _processor = processor;
            _display = display;
        }

        private CommandItem CurrentComponent
        {
            get { return _cmdComponents[_cmdComponentsIndex]; }
        }

        // COMMAND AUTOCOMPLETE
        private void AutoCompleteCommand()
        {
            _processor.ExecutePSCommand("Get-Command " + CurrentComponent.Contents + "*");
        }

        // PARAMETER AUTOCOMPLETE
        private void AutoCompleteParameter()
        {
            int index = _cmdComponentsIndex;
            string paramSeed = _cmdComponents[index].Contents.Replace(" -", "");
            CommandItemType result = CommandItemType.Undefined;
            while (CommandItemType.Command != result) {
                result = _cmdComponents[--index].Type;
            }
            _processor.ExecutePSCommand("(Get-Command " +
                _cmdComponents[index].Contents + ").Parameters.Keys | Where{$_ -like '" + paramSeed + "*'}");
            return;
        }

        // PATH AUTOCOMPLETE
        private void AutoCompletePath()
        {
            Console.WriteLine(_processor.Command);
            _processor.ExecutePSCommand("Get-ChildItem \"" + 
                CurrentComponent.Contents.Replace("\"", "").Trim() + "*\"");
        }

        // VARIABLE AUTOCOMPLETE
        private void AutoCompleteVariable()
        {
            _processor.ExecutePSCommand("Get-Variable " +
                CurrentComponent.Contents.Replace("$", "") + "*");
        }

        internal void Process(ConsoleKeyInfo keyInfo, ref CommandItemType loopType)
        {
            if (CommandItemType.Undefined == loopType) {
                _cmdComponents = dislayCmdComponents();
                // route to appropriate autcomplete handler
                switch (loopType = CurrentComponent.Type) {
                    case CommandItemType.Parameter:
                        AutoCompleteParameter();
                        break;
                    case CommandItemType.Variable:
                        AutoCompleteVariable();
                        break;
                    case CommandItemType.Path:
                        AutoCompletePath();
                        break;
                    default:
                        AutoCompleteCommand();
                        break;
                }
            }
            // If we're already in an autocomplete loop, increment loopPos appropriately
            else {
                _processor.MoveCurrentHistoryPosition(ConsoleModifiers.Shift == keyInfo.Modifiers);
            }

            // if we have results, format them and return them
            if (_processor.HasResults) {
                string separator = " ";
                string result;
                PSObject currentObject = _processor.CurrentResult;
                switch (loopType) {
                    case CommandItemType.Parameter:
                        separator = " -";
                        result = currentObject.ToString();
                        break;
                    case CommandItemType.Variable:
                        separator = " $";
                        result = currentObject.Members["Name"].Value.ToString();
                        break;
                    case CommandItemType.Path:
                        separator = " ";
                        result = "\"" + currentObject.Members["FullName"].Value.ToString() + "\"";
                        break;
                    default:
                        result = currentObject.BaseObject.ToString();
                        break;
                }
                // reconstruct display cmd from components
                string completedCmd = string.Empty;
                int cursorPos = _display.Prompt.Length;
                for (int i = 0; i < _cmdComponents.Count(); i++) {
                    if (i == _cmdComponentsIndex) {
                        completedCmd += separator + result;
                        cursorPos += completedCmd.TrimStart().Length;
                    }
                    else { completedCmd += _cmdComponents[i].Contents; }
                }
                _display.SetDisplayedCommand(completedCmd.TrimStart());
            }
            return;
        }

        // This function is used to identify chunks of autocomplete text to determine if it's a variable, path, cmdlet, etc
        // May eventually have to move this to regex to make matches more betterer.
        private static CommandItemType seedIdentification(string seed)
        {
            if (seed.Contains(" -")) { return CommandItemType.Parameter; }
            if (seed.Contains("$")) { return CommandItemType.Variable; }
            if (seed.Contains("\\") || seed.Contains(":")) { return CommandItemType.Path; }
            // This causes an issue and I can't remember why I added this.. leaving it commented 
            // for now in case I need to come back to it (2016/08/21)
            //else if (seed.Length < 4 || seed.First() == ' ') { seedType = "unknown"; }
            return CommandItemType.Command;
        }

        // This function splits text on the command line up and identifies each component
        private List<CommandItem> dislayCmdComponents()
        {
            List<CommandItem> result = new List<CommandItem>();
            int index = 0;
            int cmdLength = _display.Prompt.Length + 1;
            foreach (string item in Regex.Split(_display.DisplayedCommand, @"(?=[\s])")) {
                CommandItem itemSeed = new CommandItem() {
                    Contents = item,
                    Type = seedIdentification(item)
                };
                cmdLength += item.Length;
                if ((cmdLength > _display.CursorPosition) && (-1 == _cmdComponentsIndex)) {
                    _cmdComponentsIndex = index;
                }
                switch (itemSeed.Type) {
                    case CommandItemType.Path:
                    case CommandItemType.Undefined:
                        if (CommandItemType.Path == result.Last().Type) {
                            result.Last().Contents += itemSeed.Contents;
                            continue;
                        }
                        break;
                    default:
                        break;
                }
                result.Add(itemSeed);
                index++;
            }
            return result;
        }

        internal void Reset()
        {
            _cmdComponents = null;
            _cmdComponentsIndex = -1;
        }

        // used to store list of command components and their types
        private List<CommandItem> _cmdComponents;
        // used to store index of command compnotent being auto-completed.
        private int _cmdComponentsIndex { get; set; }
        private CommandProcessor _processor;
        private IDisplay _display;
    }
}
