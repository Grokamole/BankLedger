// Copyright 2019 Joseph Miller

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BankLedger.Common;

namespace BankLedger
{
    /// <summary>
    /// This class serves as the client interface.
    /// </summary>
    public class CommandlineInterface
    {
        /// <summary>
        /// This constructor sets up the interface for client access to ledger functionality.
        /// </summary>
        /// <param name="ledgerClientAccess">The access interface for ledger options.</param>
        public CommandlineInterface(ILedgerClientAccess ledgerClientAccess)
        {
            Debug.Assert(null != ledgerClientAccess, "Can't run without a client access interface.");
            this.ledgerClientAccess = ledgerClientAccess;
            sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
        }

        /// <summary>
        /// Runs the interface, starting with login.
        /// </summary>
        public void RunInterface()
        {
            while (RunLogin())
            { }
        }

        /// <summary>
        /// Retrieves the username and password from the user.
        /// </summary>
        /// <param name="userLoginPass">The login/password output.</param>
        /// <returns>true if the user gave</returns>
        private bool GetUsernamePassword(out Tuple<string, string> userLoginPass)
        {
            Console.Write("\n--------------\n" +
                          "Enter username\n" +
                          "--------------\n\n" +
                          "Login: ");
            string login = Console.ReadLine();
            Console.WriteLine(String.Empty);

            string password = null;

            if (!string.IsNullOrEmpty(login))
            {
                Console.Write("--------------\n" +
                              "Enter password\n" +
                              "--------------\n\n" +
                              "Password: ");
                password = Console.ReadLine();
                Console.WriteLine(String.Empty);
            }

            userLoginPass = new Tuple<string, string>(login, password);
            return ((null != login) && (LedgerConstants.EMPTY != login.Length) &&
                    (null != password) && (LedgerConstants.EMPTY != password.Length));
        }

        /// <summary>
        /// Handles getting the session and running the client program.
        /// </summary>
        private void HandleLogin()
        {
            if (GetUsernamePassword(out Tuple<string, string> userLoginPass))
            {
                if (ledgerClientAccess.Login(userLoginPass.Item1, userLoginPass.Item2, out sessionID))
                {
                    Console.Write("\nUser logged in.\n\n");
                    RunUserSession();
                }
            }
            Console.Write("\nUser not logged in.\n\n");
        }

        /// <summary>
        /// Handles the creation of a new user.
        /// </summary>
        private void HandleCreateNewUser()
        {
            if (GetUsernamePassword(out Tuple<string, string> userLoginPass))
            {
                if (ledgerClientAccess.CreateNewAccount(userLoginPass.Item1, userLoginPass.Item2))
                {
                    Console.Write("\nUser successfully created.\n\n");
                    return;
                }
            }
            Console.Write("\nUser creation failed.\n\n");
        }

        /// <summary>
        /// Run the login process.
        /// </summary>
        /// <returns>false if the user wishes to close the program. true otherwise.</returns>
        private bool RunLogin()
        {
            bool userSelectionFinished = false;
            do
            {
                DisplayLoginOptions();
                
                switch (Console.ReadLine())
                {
                    // Exit
                    case "0":
                    {
                        userSelectionFinished = true;
                    } break;
                    // Log into system
                    case "1":
                    {
                        HandleLogin();
                    } break;
                    // Create new user
                    case "2":
                    {
                        HandleCreateNewUser();
                    } break;
                    default:
                    {
                    } break;
                }
            } while (!userSelectionFinished);

            return false;
        }

        /// <summary>
        /// Prints the login options main page to console.
        /// </summary>
        private void DisplayLoginOptions()
        {
            Console.Write("-------------------------\n" +
                          "Banking Ledger Main Page\n" +
                          "-------------------------\n" +
                          "1. Log into ledger system\n" +
                          "2. Create new user\n" +
                          "0. Exit\n\n" +
                          "User choice: ");
        }

        /// <summary>
        /// Checks if the session has timed out and reports to user via console.
        /// Otherwise prints the passed in message to console.
        /// </summary>
        /// <param name="messageIfNotTimedOut"></param>
        private void HandleError(string messageIfNotTimedOut)
        {
            if (!ledgerClientAccess.SessionIDOpen(sessionID))
            {
                sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                Console.Write("\nSession timed out.\n\n");
            }
            else
            {
                Console.Write("\n" + messageIfNotTimedOut + "\n\n");
            }
        }

        /// <summary>
        /// Handles the user's balance request.
        /// </summary>
        private void HandleGetBalance()
        {
            if (ledgerClientAccess.Balance(sessionID, out decimal balance))
            {
                Console.Write("\nBalance: $" + balance.ToString("#.00") + "\n\n");
                return;
            }
            HandleError("Unknown failure getting balance.");
        }

        /// <summary>
        /// Handles the user's deposit.
        /// </summary>
        private void HandleDepositMoney()
        {
            Console.Write("\nEnter deposit amount: $");
            if (ledgerClientAccess.Deposit(sessionID, Console.ReadLine()))
            {
                Console.Write("\nDeposit successfully recorded.\n\n");
                return;
            }
            HandleError("Could not record deposit.");
        }

        /// <summary>
        /// Handles the user's withdrawal.
        /// </summary>
        private void HandleWithdrawMoney()
        {
            Console.Write("\nEnter withdrawal amount (as positive dollar amount): $");
            if (ledgerClientAccess.Withdrawal(sessionID, Console.ReadLine()))
            {
                Console.Write("\nWithdrawal successfully recorded.\n\n");
                return;
            }
            HandleError("Could not record withdrawal.");
        }

        /// <summary>
        /// Handles the user's transaction history request.
        /// </summary>
        private void HandleTransactionHistory()
        {
            if (ledgerClientAccess.TransactionHistory(sessionID, out List<LedgerTransaction> history))
            {
                Console.Write("\nTransaction History\n"+
                                "-------------------\n");

                foreach(LedgerTransaction transaction in history)
                {
                    Console.WriteLine(transaction.TransactionDate.Date.ToString() + " $" + ((transaction.Amount < 0m) ? "" : "+") + transaction.Amount.ToString("#.00"));
                }
                Console.WriteLine(String.Empty);
                return;
            }
            HandleError("Unknown failure getting transaction history.");
        }

        /// <summary>
        /// Prints the user options page to console.
        /// </summary>
        private void DisplayUserOptions()
        {
            Console.Write("----------------------\n" +
                          "User Options\n" +
                          "----------------------\n" +
                          "1. Get Balance\n" +
                          "2. Report Deposit\n" +
                          "3. Report Withdrawal\n" +
                          "4. Transaction History\n" +
                          "0. Logout\n\n" +
                          "User choice: ");
        }

        /// <summary>
        /// Runs the user's option session.
        /// </summary>
        private void RunUserSession()
        {
            bool userLogout = false;
            while((!userLogout) && (LedgerConstants.DEFAULT_INVALID_SESSIONID != sessionID))
            {
                DisplayUserOptions();
                switch(Console.ReadLine())
                {
                    // get balance
                    case "1":
                    {
                        HandleGetBalance();
                    } break;
                    // deposit money
                    case "2":
                    {
                        HandleDepositMoney();
                    } break;
                    // withdraw money
                    case "3":
                    {
                        HandleWithdrawMoney();
                    } break;
                    // get transaction history
                    case "4":
                    {
                        HandleTransactionHistory();
                    } break;
                    // logout
                    case "0":
                    {
                        userLogout = true;
                        sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                    } break;
                    default:
                    { } break;
                }
            }
            return;
        }

        /// <summary>
        /// Holds the client access object for the ledger.
        /// </summary>
        private ILedgerClientAccess ledgerClientAccess;
        /// <summary>
        /// Holds the session ID.
        /// </summary>
        private long sessionID;
    }
}
