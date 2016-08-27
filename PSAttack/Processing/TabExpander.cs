using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PSAttack.Processing
{
    internal class TabExpander
    {
        internal TabExpander(CommandProcessor processor)
        {
            _processor = processor;
        }

        internal AttackState Process(AttackState attackState)
        {
            if (null == attackState.loopType) {
                attackState.cmdComponents = dislayCmdComponents(attackState);
                // route to appropriate autcomplete handler
                switch (attackState.loopType = attackState.cmdComponents[attackState.cmdComponentsIndex].Type) {
                    case "param":
                        this.paramAutoComplete(attackState);
                        break;
                    case "variable":
                        this.variableAutoComplete(attackState);
                        break;
                    case "path":
                        this.pathAutoComplete(attackState);
                        break;
                    default:
                        this.cmdAutoComplete(attackState);
                        break;
                }
            }
            // If we're already in an autocomplete loop, increment loopPos appropriately
            else if (null != attackState.loopType) {
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
                    case "param":
                        separator = " -";
                        result = attackState.results[attackState.loopPos].ToString();
                        break;
                    case "variable":
                        separator = " $";
                        result = attackState.results[attackState.loopPos].Members["Name"].Value.ToString();
                        break;
                    case "path":
                        separator = " ";
                        result = "\"" + attackState.results[attackState.loopPos].Members["FullName"].Value.ToString() + "\"";
                        break;
                    default:
                        result = attackState.results[attackState.loopPos].BaseObject.ToString();
                        break;
                }
                // reconstruct display cmd from components
                string completedCmd = string.Empty;
                int cursorPos = attackState.promptLength;
                for (int i = 0; i < attackState.cmdComponents.Count(); i++) {
                    if (i == attackState.cmdComponentsIndex) {
                        completedCmd += separator + result;
                        cursorPos += completedCmd.TrimStart().Length;
                    }
                    else { completedCmd += attackState.cmdComponents[i].Contents; }
                }
                attackState.DisplayedCommand = completedCmd.TrimStart();
                attackState.cursorPos = cursorPos;
            }
            return attackState;
        }

        // This function is used to identify chunks of autocomplete text to determine if it's a variable, path, cmdlet, etc
        // May eventually have to move this to regex to make matches more betterer.
        private static string seedIdentification(string seed)
        {
            string seedType = "cmd";
            if (seed.Contains(" -")) {
                seedType = "param";
            }
            else if (seed.Contains("$")) {
                seedType = "variable";
            }
            else if (seed.Contains("\\") || seed.Contains(":")) {
                seedType = "path";
            }
            // This causes an issue and I can't remember why I added this.. leaving it commented 
            // for now in case I need to come back to it (2016/08/21)
            //else if (seed.Length < 4 || seed.First() == ' ')
            //{
            //    seedType = "unknown";
            //}
            return seedType;
        }

        // This function splits text on the command line up and identifies each component
        private static List<DisplayCmdComponent> dislayCmdComponents(AttackState attackState)
        {
            List<DisplayCmdComponent> results = new List<DisplayCmdComponent>();
            int index = 0;
            int cmdLength = attackState.promptLength + 1;
            foreach (string item in Regex.Split(attackState.DisplayedCommand, @"(?=[\s])")) {
                string itemType = seedIdentification(item);
                DisplayCmdComponent itemSeed = new DisplayCmdComponent() {
                    Index = index,
                    Contents = item,
                    Type = itemType
                };
                cmdLength += item.Length;
                if ((cmdLength > attackState.cursorPos) && (attackState.cmdComponentsIndex == -1)) {
                    attackState.cmdComponentsIndex = index;
                }
                if (itemType == "path" || itemType == "unknown") {
                    if (results.Last().Type == "path") {
                        results.Last().Contents +=  itemSeed.Contents;
                    }
                    else {
                        results.Add(itemSeed);
                        index++;
                    }
                }
                else {
                    results.Add(itemSeed);
                    index++;
                }
            }
            return results;
        }

        // PARAMETER AUTOCOMPLETE
        private void paramAutoComplete(AttackState attackState)
        {
            int index = attackState.cmdComponentsIndex;
            string paramSeed = attackState.cmdComponents[index].Contents.Replace(" -", "");
            string result = string.Empty; 
            while ("cmd" != result) {
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
    }
}
