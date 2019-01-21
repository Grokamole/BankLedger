// Copyright 2019 Joseph Miller

using System;

namespace BankLedger.Common
{
    /// <summary>
    /// Used for recording deposits and withdrawals.
    /// </summary>
    public class LedgerTransaction
    {
        /// <summary>
        /// Creates a ledger transaction with the set amount at the time of instantiation.
        /// </summary>
        /// <param name="amount">The amount for the transaction.</param>
        public LedgerTransaction(decimal amount)
        {
            Amount = amount;
            TransactionDate = DateTime.Now;
        }

        /// <summary>
        /// The deposit or withdrawal amount.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// The date of the deposit or withdrawal.
        /// </summary>
        public DateTime TransactionDate { get; }
    }
}
