// Copyright 2019 Joseph Miller

namespace BankLedger
{
    /// <summary>
    /// This is the main application for the bank ledger..
    /// </summary>
    class Program
    {
        /// <summary>
        /// This is the entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments [unused].</param>
        static void Main(string[] args)
        {
            CommandlineInterface cli = new CommandlineInterface(new LedgerClient(new LedgerDatabase()));
            cli.RunInterface();
        }
    }
}
