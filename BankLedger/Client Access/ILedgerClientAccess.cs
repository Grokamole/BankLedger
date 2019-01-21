// Copyright 2019 Joseph Miller

using System.Collections.Generic;
using BankLedger.Common;

namespace BankLedger
{
    /// <summary>
    /// This interface is used for the ledger client access layer.
    /// </summary>
    public interface ILedgerClientAccess
    {
        /// <summary>
        /// Creates a new account with login and password.
        /// </summary>
        /// <param name="login">The user's new login.</param>
        /// <param name="password">The user's new password.</param>
        /// <returns>true if the account was successfully created. Otherwise false.</returns>
        bool CreateNewAccount(string login, string password);

        /// <summary>
        /// Retrieves a session ID for the given login and password.
        /// </summary>
        /// <param name="login">The user's login.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="sessionID">Receives the new sessionID for the user.</param>
        /// <returns>true if the login successfully retrieved a sessionID.</returns>
        bool Login(string login, string password, out long sessionID);

        /// <summary>
        /// Logs a user out, rendering the sessionID invalid.
        /// </summary>
        /// <param name="sessionID">The user's current sessionID.</param>
        /// <returns>true if the sessionID existed and was successfully terminated.</returns>
        bool Logout(long sessionID);

        /// <summary>
        /// Used to determine whether a session ID is still open.
        /// </summary>
        /// <param name="sessionID">The session ID to check.</param>
        /// <returns>true if session still open. false otherwise.</returns>
        bool SessionIDOpen(long sessionID);

        /// <summary>
        /// Deposits money into a user account with the current sessionID.
        /// </summary>
        /// <param name="sessionID">The user's current sessionID.</param>
        /// <param name="amount">The dollar amount to deposit as a string.</param>
        /// <returns>true if the amount was successfully deposited into the acount. false otherwise.</returns>
        bool Deposit(long sessionID, string amount);

        /// <summary>
        /// Withdraws money into a user account with the current sessionID.
        /// </summary>
        /// <param name="sessionID">The user's current sessionID.</param>
        /// <param name="amount">The dollar amount to withdraw as a string.</param>
        /// <returns>true if the amount was successfully withdrawn from the account. false otherwise.</returns>
        bool Withdrawal(long sessionID, string amount);

        /// <summary>
        /// Retrieves the user's balance.
        /// </summary>
        /// <param name="sessionID">The user's current sessionID.</param>
        /// <param name="balance">Stores the user's current balance.</param>
        /// <returns>true if the balance was successfully retrieved from the account. false otherwise.</returns>
        bool Balance(long sessionID, out decimal balance);

        /// <summary>
        /// Retrieves the user's transaction history.
        /// </summary>
        /// <param name="sessionID">The user's current sessionID.</param>
        /// <param name="history">Gets set to the user's transaction history.</param>
        /// <returns>true if the transaction history was successfully retrieved from the account. false otherwise.</returns>
        bool TransactionHistory(long sessionID, out List<LedgerTransaction> history);
    }
}
