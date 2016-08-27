using System;
using PSAttack.Processing;

namespace PSAttack
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (_decryptModules) {
                throw new NotImplementedException();
            }
            CommandProcessor processor = new CommandProcessor();
            while (true) { processor.ProcessKey(Console.ReadKey()); }
        }

        private static bool ParseArgs(string[] args)
        {
            if ((1 == args.Length) && ("-d" == args[0].ToLower())) {
                _decryptModules = true;
            }
            return true;
        }

        private static bool _decryptModules = false;
    }
}