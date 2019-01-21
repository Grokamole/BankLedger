// Copyright 2019 Joseph Miller

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using BankLedger.Common;

namespace BankLedger
{
    /// <summary>
    /// This class is a database for storing ledger information while the instantiated object is alive. It does not contain persistent storage.
    /// </summary>
    public class LedgerDatabase : ILedgerDatabaseAccess
    {
        //Start configurables
            /// <summary>
            /// How many seconds the session ID should expire in.
            /// </summary>
            private const double SESSION_EXPIRATION_SECONDS = 60.0;
            /// <summary>
            /// Used for the number of seconds to wait for a mutex timeout to unlock before failing.
            /// </summary>
            private const int MUTEX_WAIT_TIME_MS = 3000;
        //End configurables

        /// <summary>
        /// Gets the user's balance by sessionID.
        /// </summary>
        /// <param name="sessionID">The sessionID to pull from.</param>
        /// <param name="balance">The user's balance, if the sessionID is valid.</param>
        /// <returns>true if the balance could be retrieved. false otherwise.</returns>
        public bool Balance(long sessionID, out decimal balance)
        {
            if (!CheckSessionID(sessionID, true))
            {
                balance = decimal.Zero;
                return false;
            }

            if (!GetUserInfoBySessionID(sessionID, out UserInfo userInfo))
            {
                balance = decimal.Zero;
                return false;
            }

            balance = userInfo.Balance;

            return true;
        }

        /// <summary>
        /// Logs the user in (if valid) and retrieves a session ID.
        /// </summary>
        /// <param name="login">The user's login.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="sessionID">The new session ID if the login and password were valid.</param>
        /// <returns>true if the user logged in successfully. false otherwise.</returns>
        public bool Login(string login, string password, out long sessionID)
        {
            if((null == login) || (null == password))
            {
                sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                return false;
            }
            if (!users.TryGetValue(login, out UserInfo userInfo))
            {
                sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                return false;
            }
            if (!userInfo.Password.Equals(PasswordHasher.GetSaltHashedPassword(password, userInfo.Password.Salt)))
            {
                sessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID;
                return false;
            }
            // free up memory associated with the user's last login since session ID will be replaced.
            Logout(userInfo.LastSessionID);

            sessionID = GetRandomSessionID();
            userInfo.LastSessionID = sessionID;

            SessionInfo sessionInfo = new SessionInfo
            {
                User = login,
                Expiration = DateTime.Now.AddSeconds(SESSION_EXPIRATION_SECONDS)
            };

            if (!sessionsMutex.WaitOne(MUTEX_WAIT_TIME_MS))
            {
                return false;
            }
            sessions.TryAdd(sessionID, sessionInfo);
            sessionsMutex.ReleaseMutex();

            return true;
        }

        /// <summary>
        /// Used to determine whether a session ID is still open.
        /// </summary>
        /// <param name="sessionID">The session ID to check.</param>
        /// <returns>true if session still open. false otherwise.</returns>
        public bool SessionIDOpen(long sessionID)
        {
            return CheckSessionID(sessionID, false);
        }

        /// <summary>
        /// Logs out a user by session ID.
        /// </summary>
        /// <param name="sessionID">The session ID to use to log out a user.</param>
        /// <returns>true if the session ID existed and was successfully removed. false otherwise.</returns>
        public bool Logout(long sessionID)
        {
            if (!sessionsMutex.WaitOne(MUTEX_WAIT_TIME_MS))
            {
                return false;
            }
            bool removed = sessions.Remove(sessionID);
            sessionsMutex.ReleaseMutex();
            return removed;
        }

        /// <summary>
        /// Creates a new user account in the database.
        /// </summary>
        /// <param name="login">The user's login name.</param>
        /// <param name="password">The user's login password.</param>
        /// <returns>true if the account could be created. false otherwise.</returns>
        public bool CreateNewAccount(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return false;
            }
            if (users.ContainsKey(login))
            {
                return false;
            }
            UserInfo userInfo = new UserInfo
            {
                User = login,
                Balance = decimal.Zero,
                LastSessionID = LedgerConstants.DEFAULT_INVALID_SESSIONID,
                Password = PasswordHasher.GetSaltHashedPassword(password),
                Transactions = new List<LedgerTransaction>()
            };

            return users.TryAdd(login, userInfo);
        }

        /// <summary>
        /// Deposits money into the user's account.
        /// </summary>
        /// <param name="sessionID">The current sessionID.</param>
        /// <param name="amount">The amount to deposit.</param>
        /// <returns>true if the money was succesfully deposited. otherwise false.</returns>
        public bool Deposit(long sessionID, decimal amount)
        {
            if (!CheckSessionID(sessionID, true))
            {
                return false;
            }

            if (!GetUserInfoBySessionID(sessionID, out UserInfo userInfo))
            {
                return false;
            }

            if (decimal.Zero == amount)
            {
                return false;
            }

            if (amount < decimal.Zero)
            {
                return false;
            }

            userInfo.Balance += amount;
            userInfo.Transactions.Add(new LedgerTransaction(amount));

            return true;
        }

        /// <summary>
        /// Retrieves the user's transaction history by sessionID.
        /// </summary>
        /// <param name="sessionID">The current session ID.</param>
        /// <param name="history">The transaction history.</param>
        /// <returns>true if the transaction history could be retrieved. Otherwise false.</returns>
        public bool TransactionHistory(long sessionID, out List<LedgerTransaction> history)
        {
            if (!CheckSessionID(sessionID, true))
            {
                history = new List<LedgerTransaction>();
                return false;
            }

            if (!GetUserInfoBySessionID(sessionID, out UserInfo userInfo))
            {
                history = new List<LedgerTransaction>();
                return false;
            }

            history = new List<LedgerTransaction>(userInfo.Transactions);

            return true;
        }

        /// <summary>
        /// Withdraw money from the account via session ID.
        /// </summary>
        /// <param name="sessionID">The ID of the current session.</param>
        /// <param name="amount">The amount to withdraw from the account.</param>
        /// <returns>true if the amount was withdrawn. Otherwise false.</returns>
        public bool Withdrawal(long sessionID, decimal amount)
        {
            if (!CheckSessionID(sessionID, true))
            {
                return false;
            }

            if (!GetUserInfoBySessionID(sessionID, out UserInfo userInfo))
            {
                return false;
            }

            if (amount < decimal.Zero)
            {
                return false;
            }

            userInfo.Balance -= amount;
            userInfo.Transactions.Add(new LedgerTransaction(-amount));

            return true;
        }

        /// <summary>
        /// Used to store the values of a session for the session Dictionary.
        /// </summary>
        private class SessionInfo
        {
            /// <summary>
            /// The associated user.
            /// </summary>
            public string User { get; set; }
            /// <summary>
            /// When this info expires.
            /// </summary>
            public DateTime Expiration { get; set; }
        }

        /// <summary>
        /// Used to store the values of a user.
        /// </summary>
        private class UserInfo
        {
            /// <summary>
            /// The user's login.
            /// </summary>
            public string User { get; set; }
            /// <summary>
            /// The user's password.
            /// </summary>
            public PasswordHasher.HashedPassword Password { get; set; }
            /// <summary>
            /// The user's current balance.
            /// </summary>
            public decimal Balance { get; set; }
            /// <summary>
            /// The user's last sessionID.
            /// </summary>
            public long LastSessionID { get; set; }
            /// <summary>
            /// The user's list of transactions.
            /// </summary>
            public List<LedgerTransaction> Transactions { get; set; }
        }

        /// <summary>
        /// Checks that the session ID exists and is current. If it exists and is not current,
        /// this will remove it from the Dictionary.
        /// </summary>
        /// <param name="sessionID">The session ID to check</param>
        /// <param name="updateExpiration">Whether the session's expiration should be updated</param>
        /// <returns>true if the sessionID is valid. false otherwise.</returns>
        private bool CheckSessionID(long sessionID, bool updateExpiration)
        {
            if (!sessionsMutex.WaitOne(MUTEX_WAIT_TIME_MS))
            {
                return false;
            }
            if (!sessions.ContainsKey(sessionID))
            {
                sessionsMutex.ReleaseMutex();
                return false;
            }
            SessionInfo info = sessions[sessionID];
            if (DateTime.Now > info.Expiration)
            {
                sessions.Remove(sessionID);
                sessionsMutex.ReleaseMutex();
                return false;
            }

            if(updateExpiration)
            {
                info.Expiration = DateTime.Now.AddSeconds(SESSION_EXPIRATION_SECONDS);
            }
            sessionsMutex.ReleaseMutex();
            return true;
        }

        /// <summary>
        /// Returns a random session ID that does not exist in the sessions Dictionary.
        /// </summary>
        /// <returns>An unused session ID value not equal to DEFAULT_INVALID_SESSIONID unless the thread times out.</returns>
        private long GetRandomSessionID()
        {
            byte[] randomBytes;
            long value;
            if (!sessionsMutex.WaitOne(MUTEX_WAIT_TIME_MS))
            {
                return LedgerConstants.DEFAULT_INVALID_SESSIONID;
            }
            do
            {
                PasswordHasher.GetRandomCryptoBytes(LONG_BYTE_SIZE, out randomBytes);
                value = BitConverter.ToInt64(randomBytes, LedgerConstants.FIRST_ELEMENT);
            } while ((sessions.ContainsKey(value)) || (value == LedgerConstants.DEFAULT_INVALID_SESSIONID));
            sessionsMutex.ReleaseMutex();
            return value;
        }

        /// <summary>
        /// Gets the current UserInfo from the session ID.
        /// </summary>
        /// <param name="sessionID">The session ID to retrieve the user from.</param>
        /// <param name="userInfo">The user info.</param>
        /// <returns>true if the session ID yielded a user. false otherwise.</returns>
        private bool GetUserInfoBySessionID(long sessionID, out UserInfo userInfo)
        {
            if (!sessionsMutex.WaitOne(MUTEX_WAIT_TIME_MS))
            {
                userInfo = new UserInfo { };
                return false;
            }
            if (!sessions.TryGetValue(sessionID, out SessionInfo info))
            {
                sessionsMutex.ReleaseMutex();
                userInfo = new UserInfo { };
                return false;
            }
            sessionsMutex.ReleaseMutex();
            return users.TryGetValue(info.User, out userInfo);
        }

        /// <summary>
        /// Used to denote empty data structures.
        /// </summary>
        private const int EMPTY = 0;
        /// <summary>
        /// Used to denote the number of bytes in a long.
        /// </summary>
        private const int LONG_BYTE_SIZE = 8;
        /// <summary>
        /// A secure RNG provider for sessionID generation
        /// </summary>
        private static RNGCryptoServiceProvider randomServiceProvider = new RNGCryptoServiceProvider();

        /// <summary>
        /// Used for associating a sessionID with the current SessionInfo.
        /// </summary>
        private readonly Dictionary<long, SessionInfo> sessions = new Dictionary<long, SessionInfo>();
        /// <summary>
        /// Used for associating a user with their info.
        /// </summary>
        private readonly ConcurrentDictionary<string, UserInfo> users = new ConcurrentDictionary<string, UserInfo>();
        /// <summary>
        /// Used for multiple sessions dictionary access.
        /// </summary>
        private Mutex sessionsMutex = new Mutex();
    }
}
