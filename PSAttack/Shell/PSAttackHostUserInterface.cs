﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;

namespace PSAttack.Shell
{
    internal class PSAttackHostUserInterface : PSHostUserInterface
    {
        // Function used for PromptForCredential
        private PSCredential GetCreds(string caption, string message)
        {
            Console.WriteLine(caption);
            Console.WriteLine(message);
            Console.Write("Enter Username (domain\\user): ");
            string userName = Console.ReadLine();
            Console.Write("Enter Pass: ");
            ConsoleKeyInfo info = Console.ReadKey(true);
            string password = "";
            while (info.Key != ConsoleKey.Enter) {
                if (info.Key != ConsoleKey.Backspace) {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace) {
                    if (!string.IsNullOrEmpty(password)) {
                        password = password.Substring(0, password.Length - 1);
                        int pos = Console.CursorLeft;
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            SecureString secPasswd = new SecureString();
            foreach (char c in password) { secPasswd.AppendChar(c); }
            secPasswd.MakeReadOnly();
            return new PSCredential(userName, secPasswd);
        }

        public override PSHostRawUserInterface RawUI
        {
            get { return PSAttackRawUI; }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message,
            Collection<FieldDescription> descriptions)
        {
            if (null == descriptions) { return null; }
            throw new NotImplementedException();
            //return (null == descriptions) ? null : GetParameters(descriptions);
        }

        //private Dictionary<string, PSObject> GetParameters(Collection<FieldDescription> descriptions)
        //{
        //    Dictionary<string, PSObject> result = new Dictionary<string, PSObject>();
        //    PSParamType parm = new PSParamType();
        //    foreach (FieldDescription descr in descriptions) {
        //        PSParameter addedParameter = new PSParameter() {
        //            Name = descr.Name,
        //            Category = (descr.IsMandatory) ? "Required" : "Optional",
        //            DefaultValue = descr.DefaultValue,
        //            Description = descr.HelpMessage,
        //            Type = Type.GetType(descr.ParameterAssemblyFullName)
        //        };
        //        string canonicParameterName = addedParameter.Name.ToLower();
        //        if (canonicParameterName == "file" || canonicParameterName == "filename") {
        //            addedParameter.IsFileName = true;
        //        }
        //        if (canonicParameterName == "credential") {
        //            addedParameter.IsCredential = true;
        //        }
        //        parm.Properties.Add(addedParameter);
        //    }
        //    return result;
        //}

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
            Console.ForegroundColor = PSColors.outputText;
            Console.Write(message);
        }

        public override void Write(string message)
        {
            Console.ForegroundColor = PSColors.outputText;
            Console.Write(message);
        }

        public override void WriteDebugLine(string message)
        {
            Console.ForegroundColor = PSColors.debugText;
            Console.WriteLine("DEBUG: {0}", message);
            Console.ForegroundColor = PSColors.outputText;
        }

        public override void WriteErrorLine(string message)
        {
            Console.ForegroundColor = PSColors.errorText;
            Console.WriteLine("ERROR: {0}", message);
            Console.ForegroundColor = PSColors.outputText;
        }

        public override void WriteLine(string message)
        {
            Console.ForegroundColor = PSColors.outputText;
            Console.WriteLine(message);
        }

        public override void WriteVerboseLine(string message)
        {
            Console.ForegroundColor = PSColors.outputText;
            Console.WriteLine(message);
        }

        public override void WriteWarningLine(string message)
        {
            Console.ForegroundColor = PSColors.warningText;
            Console.WriteLine("WARNING: {0}", message);
            Console.ForegroundColor = PSColors.outputText;
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            return;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            Console.ForegroundColor = PSColors.outputText;
            Console.WriteLine(caption);
            Console.WriteLine(message);
            int choiceInt = defaultChoice;
            foreach (ChoiceDescription choice in choices) {
                Console.ForegroundColor = PSColors.outputText;
                if (choices.IndexOf(choice) == defaultChoice) {
                    Console.ForegroundColor = PSColors.warningText;
                }
                Console.WriteLine("[{0}] {1} ", choices.IndexOf(choice), choice.Label.ToString().Replace("&",""));
            }
            Console.WriteLine("Default is: {0}", choices[defaultChoice].Label.ToString().Replace("&",""));
            Console.Write("\nEnter your choice: ");
            string choiceStr = Console.ReadLine();
            return int.Parse(choiceStr);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return GetCreds(caption, message);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            return GetCreds(caption, message);
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }

        private PSAttackRawUserInterface PSAttackRawUI = new PSAttackRawUserInterface();
    }
}