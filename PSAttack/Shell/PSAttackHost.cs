using System;
using System.Management.Automation.Host;
using System.Globalization;
using System.Threading;
using PSAttack.Shell;

namespace PSAttack
{
    internal class PSAttackHost : PSHost
    {
        public override CultureInfo CurrentCulture
        {
            get { return _originalCultureInfo; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return _originalUICultureInfo; }
        }

        public override Guid InstanceId
        {
            get { return _gid; }
        }

        public override string Name
        {
            get { return "PS ATTACK!!!"; }
        }

        public override PSHostUserInterface UI
        {
            get { return PSAttackUI; }
        }

        public override Version Version
        {
            // return the powershell version supported
            get { return _version; }
        }

        public override void EnterNestedPrompt()
        {
            Console.WriteLine("Entering Nested Prompt");
        }

        public override void ExitNestedPrompt()
        {
            Console.WriteLine("Exiting Nested Prompt");
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override void SetShouldExit(int exitCode)
        {
            return;
        }

        private PSAttackHostUserInterface PSAttackUI = new PSAttackHostUserInterface();
        private Guid _gid = Guid.NewGuid();
        private CultureInfo _originalCultureInfo = Thread.CurrentThread.CurrentCulture;
        private CultureInfo _originalUICultureInfo = Thread.CurrentThread.CurrentUICulture;
        private static readonly Version _version = new Version(3, 0, 0, 0);
    }
}
