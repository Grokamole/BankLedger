// Copyright 2019 Joseph Miller

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using BankLedger.Common;

namespace BankLedger
{
    /// <summary>
    /// The client access layer for receiving direct application access to a ledger.
    /// </summary>
    public class LedgerClient : ILedgerClientAccess
    {
        /// <summary>
        /// This sets the client up to use a particular database access throughout the run.
        /// </summary>
        /// <param name="database">The database access object to use.</param>
        public LedgerClient(ILedgerDatabaseAccess database)
        {
            ledgerDatabase = database;
        }

        /// <summary>
        /// Retrieves the balance from the database.
        /// </summary>
        /// <param name="sessionID">The ID of the session.</param>
        /// <param name="balance">Receives the balance.</param>
        /// <returns>true if the balance was successfully received. false otherwise.</returns>
        public bool Balance(long sessionID, out decimal balance)
        {
            return ledgerDatabase.Balance(sessionID, out balance);
        }

        /// <summary>
        /// Retrieves a new session ID for the user.
        /// </summary>
        /// <param name="login">The user's login.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="sessionID">Receives session ID.</param>
        /// <returns>true if the session was successfully opened. false otherwise.</returns>
        public bool Login(string login, string password, out long sessionID)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                return false;
            }

            return ledgerDatabase.Login(login, password, out sessionID);
        }

        /// <summary>
        /// Verifies that the session ID is still valid.
        /// </summary>
        /// <param name="sessionID">The session ID to check.</param>
        /// <returns>true if the session is open. false otherwise.</returns>
        public bool SessionIDOpen(long sessionID)
        {
            return ledgerDatabase.SessionIDOpen(sessionID);
        }

        /// <summary>
        /// Logs out the session.
        /// </summary>
        /// <param name="sessionID">The session to log out of.</param>
        /// <returns>true if the session existed and was logged out of. false otherwise.</returns>
        public bool Logout(long sessionID)
        {
            return ledgerDatabase.Logout(sessionID);
        }

        /// <summary>
        /// Creates a new account in the system with the login and password.
        /// </summary>
        /// <param name="login">The user's new login.</param>
        /// <param name="password">The user's new password.</param>
        /// <returns>true if the account was created. false if the account could not be.</returns>
        public bool CreateNewAccount(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            return ledgerDatabase.CreateNewAccount(login, password);
        }

        /// <summary>
        /// Creates an entry for depositing an amount into the ledger.
        /// </summary>
        /// <param name="sessionID">The id of the current session.</param>
        /// <param name="amount">The amount of deposit to record.</param>
        /// <returns>true if the amount was deposited. false otherwise.</returns>
        public bool Deposit(long sessionID, string amount)
        {
            if (!GetDollarsFromString(amount, out decimal value))
            {
                return false;
            }
            
            return ledgerDatabase.Deposit(sessionID, value);
        }

        /// <summary>
        /// Retrieves the transaction history for the user.
        /// </summary>
        /// <param name="sessionID">The id of the current session.</param>
        /// <param name="history">Receives the transaction history.</param>
        /// <returns>true if the history was received. false otherwise.</returns>
        public bool TransactionHistory(long sessionID, out List<LedgerTransaction> history)
        {            
            return ledgerDatabase.TransactionHistory(sessionID, out history);
        }

        /// <summary>
        /// Records a withdrawal in the ledger.
        /// </summary>
        /// <param name="sessionID">The session ID to use.</param>
        /// <param name="amount">The amount of the withdrawal.</param>
        /// <returns>true if the amount was received. false otherwise.</returns>
        public bool Withdrawal(long sessionID, string amount)
        {
            if (!GetDollarsFromString(amount, out decimal value))
            {
                return false;
            }

            return ledgerDatabase.Withdrawal(sessionID, value);
        }

        /// <summary>
        /// Converts a string into the decimal dollar equivalent if formatted correctly.
        /// </summary>
        /// <param name="checkString">The string to parse into a decimal.</param>
        /// <param name="dollars">The dollar amount.</param>
        /// <returns>true if the checkString could be converted correctly. false if it could not be converted.</returns>
        private bool GetDollarsFromString(string checkString, out decimal dollars)
        {
            Debug.Assert(checkString != null, "checkString should not be null.");

            if (LedgerConstants.EMPTY == checkString.Length)
            {
                dollars = decimal.Zero;
                return false;
            }

            // find first decimal point, if it exists
            int decimalPoint = checkString.IndexOf(".");
            // if decimal point exists
            if (decimalPoint != LedgerConstants.CHARACTER_NOT_FOUND)
            {
                // if decimal point is either last or offers greater precision than 2 decimal places, it is invalid
                if ((decimalPoint == (checkString.Length - 1)) || (decimalPoint < (checkString.Length - 3)))
                {
                    dollars = decimal.Zero;
                    return false;
                }
                // if there is a second decimal point, it is invalid
                if (checkString.IndexOf(".", decimalPoint + 1) != LedgerConstants.CHARACTER_NOT_FOUND)
                {
                    dollars = decimal.Zero;
                    return false;
                }
            }

            return decimal.TryParse(checkString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                                    CultureInfo.CreateSpecificCulture("en-US"), out dollars);
        }

        /// <summary>
        /// The database to access.
        /// </summary>
        private readonly ILedgerDatabaseAccess ledgerDatabase;
    }
}
