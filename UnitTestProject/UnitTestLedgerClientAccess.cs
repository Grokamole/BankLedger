// Copyright 2019 Joseph Miller

using Microsoft.VisualStudio.TestTools.UnitTesting;
using BankLedger;
using System.Collections.Generic;
using BankLedger.Common;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTestLedgerClientAccess
    {
        [TestInitialize]
        public void TestInitialize()
        {
            processor = new LedgerClient(new LedgerDatabase());
        }

        [TestMethod]
        public void TestCreateNewAccount()
        {
            Assert.IsTrue(processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD), "Basic case invalid.");
            Assert.IsTrue(processor.Login(TEST_LOGIN, TEST_PASSWORD, out long sessionID), "Basic case database did not exist.");
            Assert.IsFalse(processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD), "Duplicate login added.");
            Assert.IsFalse(processor.CreateNewAccount(null, null), "null user/pass accepted.");
            Assert.IsFalse(processor.CreateNewAccount("newacct1", null), "null pass accepted.");
            Assert.IsFalse(processor.CreateNewAccount(null, "newpass1"), "null user accepted.");
        }

        [TestMethod]
        public void TestLogin()
        {
            processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            Assert.IsTrue(processor.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID), "Basic case invalid.");
            Assert.IsFalse(processor.Login(TEST_LOGIN, "invalidpass", out sessionID), "Incorrect password accepted.");
            Assert.IsFalse(processor.Login("invalidlogin", TEST_PASSWORD, out sessionID), "Incorrect login accepted.");
            Assert.IsFalse(processor.Login(TEST_LOGIN, null, out sessionID), "null password accepted.");
            Assert.IsFalse(processor.Login(null, TEST_PASSWORD, out sessionID), "null login accepted.");
            Assert.IsFalse(processor.Login(null, null, out sessionID), "null user/pass accepted.");
        }

        [TestMethod]
        public void TestDeposit()
        {
            processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            processor.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            Assert.IsTrue(processor.Deposit(sessionID, "1.00"), "Basic case invalid.");
            decimal balance;
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: basic case.");
            Assert.IsFalse(processor.Deposit(sessionID, "0.00"), "Deposited zero.");
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: zero case.");
            Assert.IsFalse(processor.Deposit(sessionID, "-1.00"), "Deposited negative value.");
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: negative case.");
            Assert.IsTrue(processor.Deposit(sessionID, "1.00"), "Could not deposit second value.");
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == 2m), "Balance incorrect: 2nd deposited value.");
            Assert.IsFalse(processor.Deposit(sessionID, "0."), "Parsed with ending decimal point.");
            Assert.IsFalse(processor.Deposit(sessionID, "1.1.1"), "Parsed with second decimal point.");
            Assert.IsFalse(processor.Deposit(sessionID, "a1.00"), "Parsed with letters.");
        }

        [TestMethod]
        public void TestWithdrawal()
        {
            processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            processor.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            processor.Deposit(sessionID, "1.00");
            Assert.IsTrue(processor.Withdrawal(sessionID, "1.00"), "Basic case invalid.");
            decimal balance;
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.Zero), "Invalid balance: Basic case.");
            Assert.IsFalse(processor.Withdrawal(sessionID, "-1.00"), "Withdrew negative money from zeroed account.");
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.Zero), "Invalid balance: Negative case with zero balance.");
            processor.Deposit(sessionID, "1.00");
            Assert.IsFalse(processor.Withdrawal(sessionID, "-1.00"), "Withdrew negative money from positive account.");
            processor.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Invalid balance: Negative case with positive balance.");
            Assert.IsFalse(processor.Withdrawal(sessionID, "0."), "Parsed with ending decimal point.");
            Assert.IsFalse(processor.Withdrawal(sessionID, "0.1.1"), "Parsed with second decimal point.");
            Assert.IsFalse(processor.Withdrawal(sessionID, "a1.00"), "Parsed with letters.");
        }

        [TestMethod]
        public void TestBalance()
        {
            processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            processor.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            decimal balance;
            Assert.IsTrue(processor.Balance(sessionID, out balance), "Could not get initial balance.");
            Assert.IsTrue(balance == decimal.Zero);
            processor.Deposit(sessionID, "1.00");
            Assert.IsTrue(processor.Balance(sessionID, out balance), "Could not get deposited balance.");
            Assert.IsTrue(balance == decimal.One);
            processor.Withdrawal(sessionID, "1.00");
            Assert.IsTrue(processor.Balance(sessionID, out balance), "Could not get withdrawn balance.");
            Assert.IsTrue(balance == decimal.Zero);
        }

        [TestMethod]
        public void TestTransactionHistory()
        {
            processor.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            processor.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            List<LedgerTransaction> transactions;
            Assert.IsTrue(processor.TransactionHistory(sessionID, out transactions), "Could not get initial transactions.");
            Assert.IsTrue(transactions != null, "Transactions 0 null.");
            Assert.IsTrue(transactions.Count == 0, "Transactions 0 contains transaction.");
            processor.Deposit(sessionID, "1.00");
            Assert.IsTrue(processor.TransactionHistory(sessionID, out transactions), "Could not get 1st transaction.");
            Assert.IsTrue(transactions != null, "Transactions 1 null.");
            Assert.IsTrue(transactions.Count == 1, "Transactions 1 contains incorrect number of transactions.");
            Assert.IsTrue(transactions[0].Amount == decimal.One, "Transactions 1 contains incorrect transaction.");
            processor.Withdrawal(sessionID, "1.00");
            Assert.IsTrue(processor.TransactionHistory(sessionID, out transactions), "Could not get 2nd transaction.");
            Assert.IsTrue(transactions != null, "Transactions 2 null.");
            Assert.IsTrue(transactions.Count == 2, "Transactions 2 contains incorrect number of transactions.");
            Assert.IsTrue(transactions[1].Amount == decimal.MinusOne, "Transactions 2 contains incorrect transaction.");
            transactions.Add(new LedgerTransaction(decimal.One));
            processor.TransactionHistory(sessionID, out transactions);
            Assert.IsTrue(transactions.Count == 2);
        }

        private ILedgerClientAccess processor;
        private const string TEST_LOGIN = "test";
        private const string TEST_PASSWORD = "test";
    }
}
