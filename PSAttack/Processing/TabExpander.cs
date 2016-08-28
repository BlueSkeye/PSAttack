using System;
using System.Collections.Generic;
using System.Linq;
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

        internal AttackState Process(AttackState attackState)
        {
            if (DisplayCmdComponent.CommponentType.Undefined == attackState.loopType) {
                attackState.cmdComponents = dislayCmdComponents(attackState);
                // route to appropriate autcomplete handler
                switch (attackState.loopType = attackState.cmdComponents[attackState.cmdComponentsIndex].Type) {
                    case DisplayCmdComponent.CommponentType.Parameter:
                        paramAutoComplete(attackState);
                        break;
                    case DisplayCmdComponent.CommponentType.Variable:
                        variableAutoComplete(attackState);
                        break;
                    case DisplayCmdComponent.CommponentType.Path:
                        pathAutoComplete(attackState);
                        break;
                    default:
                        cmdAutoComplete(attackState);
                        break;
                }
            }
            // If we're already in an autocomplete loop, increment loopPos appropriately
            else {
                if (attackState.keyInfo.Modifiers == ConsoleModifiers.Shift) {
                    attackState.loopPos -= 1;
                    // loop around if we're at the beginning
                    if (0 > attackState.loopPos) {
                        attackState.loopPos = attackState.results.Count - 1;
                    }
                }
                else {
                    attackState.loopPos += 1;
                    // loop around if we reach the end
                    if (attackState.loopPos >= attackState.results.Count) {
                        attackState.loopPos = 0;
                    }
                }
            }

            // if we have results, format them and return them
            if (0 < attackState.results.Count) {
                string separator = " ";
                string result;
                switch (attackState.loopType) {
                    case DisplayCmdComponent.CommponentType.Parameter:
                        separator = " -";
                        result = attackState.results[attackState.loopPos].ToString();
                        break;
                    case DisplayCmdComponent.CommponentType.Variable:
                        separator = " $";
                        result = attackState.results[attackState.loopPos].Members["Name"].Value.ToString();
                        break;
                    case DisplayCmdComponent.CommponentType.Path:
                        separator = " ";
                        result = "\"" + attackState.results[attackState.loopPos].Members["FullName"].Value.ToString() + "\"";
                        break;
                    default:
                        result = attackState.results[attackState.loopPos].BaseObject.ToString();
                        break;
                }
                // reconstruct display cmd from components
                string completedCmd = string.Empty;
                int cursorPos = _display.Prompt.Length;
                for (int i = 0; i < attackState.cmdComponents.Count(); i++) {
                    if (i == attackState.cmdComponentsIndex) {
                        completedCmd += separator + result;
                        cursorPos += completedCmd.TrimStart().Length;
                    }
                    else { completedCmd += attackState.cmdComponents[i].Contents; }
                }
                _display.SetDisplayedCommand(completedCmd.TrimStart());
            }
            return attackState;
        }

        // This function is used to identify chunks of autocomplete text to determine if it's a variable, path, cmdlet, etc
        // May eventually have to move this to regex to make matches more betterer.
        private static DisplayCmdComponent.CommponentType seedIdentification(string seed)
        {
            if (seed.Contains(" -")) { return DisplayCmdComponent.CommponentType.Parameter; }
            if (seed.Contains("$")) { return DisplayCmdComponent.CommponentType.Variable; }
            if (seed.Contains("\\") || seed.Contains(":")) { return DisplayCmdComponent.CommponentType.Path; }
            // This causes an issue and I can't remember why I added this.. leaving it commented 
            // for now in case I need to come back to it (2016/08/21)
            //else if (seed.Length < 4 || seed.First() == ' ') { seedType = "unknown"; }
            return DisplayCmdComponent.CommponentType.Command;
        }

        // This function splits text on the command line up and identifies each component
        private List<DisplayCmdComponent> dislayCmdComponents(AttackState attackState)
        {
            List<DisplayCmdComponent> result = new List<DisplayCmdComponent>();
            int index = 0;
            int cmdLength = _display.Prompt.Length + 1;
            foreach (string item in Regex.Split(_display.DisplayedCommand, @"(?=[\s])")) {
                DisplayCmdComponent itemSeed = new DisplayCmdComponent() {
                    Index = index,
                    Contents = item,
                    Type = seedIdentification(item)
                };
                cmdLength += item.Length;
                if ((cmdLength > _display.CursorPosition) && (attackState.cmdComponentsIndex == -1)) {
                    attackState.cmdComponentsIndex = index;
                }
                switch (itemSeed.Type) {
                    case DisplayCmdComponent.CommponentType.Path:
                    case DisplayCmdComponent.CommponentType.Undefined:
                        if (DisplayCmdComponent.CommponentType.Path == result.Last().Type) {
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

        // PARAMETER AUTOCOMPLETE
        private void paramAutoComplete(AttackState attackState)
        {
            int index = attackState.cmdComponentsIndex;
            string paramSeed = attackState.cmdComponents[index].Contents.Replace(" -", "");
            DisplayCmdComponent.CommponentType result = DisplayCmdComponent.CommponentType.Undefined;
            while (DisplayCmdComponent.CommponentType.Command != result) {
                index -= 1;
                result = attackState.cmdComponents[index].Type;
            }
            string paramCmd = attackState.cmdComponents[index].Contents;
            _processor.ExecutePSCommand("(Get-Command " + paramCmd + ").Parameters.Keys | Where{$_ -like '" + paramSeed + "*'}");
            return;
        }

        // VARIABLE AUTOCOMPLETE
        private void variableAutoComplete(AttackState attackState)
        {
            _processor.ExecutePSCommand("Get-Variable " + attackState.cmdComponents[attackState.cmdComponentsIndex].Contents.Replace("$", "") + "*");
        }

        // PATH AUTOCOMPLETE
        private void pathAutoComplete(AttackState attackState)
        {
            Console.WriteLine(attackState.Command);
            _processor.ExecutePSCommand("Get-ChildItem \"" + attackState.cmdComponents[attackState.cmdComponentsIndex].Contents.Replace("\"", "").Trim() + "*\"");
        }
                
        // COMMAND AUTOCOMPLETE
        private void cmdAutoComplete(AttackState attackState)
        {
            _processor.ExecutePSCommand("Get-Command " + attackState.cmdComponents[attackState.cmdComponentsIndex].Contents + "*");
        }

        private CommandProcessor _processor;
        private IDisplay _display;
    }
}
