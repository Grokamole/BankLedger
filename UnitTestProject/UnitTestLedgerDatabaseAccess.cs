// Copyright 2019 Joseph Miller

using Microsoft.VisualStudio.TestTools.UnitTesting;
using BankLedger;
using System.Collections.Generic;
using BankLedger.Common;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTestLedgerDatabaseAccess
    {
        [TestInitialize]
        public void TestInitializer()
        {
            database = new LedgerDatabase();
        }

        [TestMethod]
        public void TestCreateNewAccount()
        {
            Assert.IsTrue(database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD), "Basic case invalid.");
            Assert.IsTrue(database.Login(TEST_LOGIN, TEST_PASSWORD, out long sessionID), "Basic case database did not exist.");
            Assert.IsFalse(database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD), "Duplicate login added.");
            Assert.IsFalse(database.CreateNewAccount(null, null), "null user/pass accepted.");
            Assert.IsFalse(database.CreateNewAccount("newacct1", null), "null pass accepted.");
            Assert.IsFalse(database.CreateNewAccount(null, "newpass1"), "null user accepted.");
        }

        [TestMethod]
        public void TestLogin()
        {
            database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            Assert.IsTrue(database.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID), "Basic case invalid.");
            Assert.IsFalse(database.Login(TEST_LOGIN, "invalidpass", out sessionID), "Incorrect password accepted.");
            Assert.IsFalse(database.Login("invalidlogin", TEST_PASSWORD, out sessionID), "Incorrect login accepted.");
            Assert.IsFalse(database.Login(TEST_LOGIN, null, out sessionID), "null password accepted.");
            Assert.IsFalse(database.Login(null, TEST_PASSWORD, out sessionID), "null login accepted.");
            Assert.IsFalse(database.Login(null, null, out sessionID), "null user/pass accepted.");
        }

        [TestMethod]
        public void TestDeposit()
        {
            database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            database.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            Assert.IsTrue(database.Deposit(sessionID, decimal.One), "Basic case invalid.");
            decimal balance;
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: basic case.");
            Assert.IsFalse(database.Deposit(sessionID, decimal.Zero), "Deposited zero.");
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: zero case.");
            Assert.IsFalse(database.Deposit(sessionID, decimal.MinusOne), "Deposited negative value.");
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Balance incorrect: negative case.");
            Assert.IsTrue(database.Deposit(sessionID, decimal.One), "Could not deposit second value.");
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == 2m), "Balance incorrect: 2nd deposited value.");
        }

        [TestMethod]
        public void TestWithdrawal()
        {
            database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            database.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            database.Deposit(sessionID, decimal.One);
            Assert.IsTrue(database.Withdrawal(sessionID, decimal.One), "Basic case invalid.");
            decimal balance;
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.Zero), "Invalid balance: Basic case.");
            Assert.IsFalse(database.Withdrawal(sessionID, decimal.MinusOne), "Withdrew negative money from zeroed account.");
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.Zero), "Invalid balance: Negative case with zero balance.");
            database.Deposit(sessionID, decimal.One);
            Assert.IsFalse(database.Withdrawal(sessionID, decimal.MinusOne), "Withdrew negative money from positive account.");
            database.Balance(sessionID, out balance);
            Assert.IsTrue((balance == decimal.One), "Invalid balance: Negative case with positive balance.");
        }

        [TestMethod]
        public void TestBalance()
        {
            database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            database.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            decimal balance;
            Assert.IsTrue(database.Balance(sessionID, out balance), "Could not get initial balance.");
            Assert.IsTrue(balance == decimal.Zero);
            database.Deposit(sessionID, decimal.One);
            Assert.IsTrue(database.Balance(sessionID, out balance), "Could not get deposited balance.");
            Assert.IsTrue(balance == decimal.One);
            database.Withdrawal(sessionID, decimal.One);
            Assert.IsTrue(database.Balance(sessionID, out balance), "Could not get withdrawn balance.");
            Assert.IsTrue(balance == decimal.Zero);
        }

        [TestMethod]
        public void TestTransactionHistory()
        {
            database.CreateNewAccount(TEST_LOGIN, TEST_PASSWORD);
            long sessionID;
            database.Login(TEST_LOGIN, TEST_PASSWORD, out sessionID);

            List<LedgerTransaction> transactions;
            Assert.IsTrue(database.TransactionHistory(sessionID, out transactions), "Could not get initial transactions.");
            Assert.IsTrue(transactions != null, "Transactions 0 null.");
            Assert.IsTrue(transactions.Count == 0, "Transactions 0 contains transaction.");
            database.Deposit(sessionID, decimal.One);
            Assert.IsTrue(database.TransactionHistory(sessionID, out transactions), "Could not get 1st transaction.");
            Assert.IsTrue(transactions != null, "Transactions 1 null.");
            Assert.IsTrue(transactions.Count == 1, "Transactions 1 contains incorrect number of transactions.");
            Assert.IsTrue(transactions[0].Amount == decimal.One, "Transactions 1 contains incorrect transaction.");
            database.Withdrawal(sessionID, decimal.One);
            Assert.IsTrue(database.TransactionHistory(sessionID, out transactions), "Could not get 2nd transaction.");
            Assert.IsTrue(transactions != null, "Transactions 2 null.");
            Assert.IsTrue(transactions.Count == 2, "Transactions 2 contains incorrect number of transactions.");
            Assert.IsTrue(transactions[1].Amount == decimal.MinusOne, "Transactions 2 contains incorrect transaction.");
            transactions.Add(new LedgerTransaction(decimal.One));
            database.TransactionHistory(sessionID, out transactions);
            Assert.IsTrue(transactions.Count == 2);
        }

        private LedgerDatabase database;
        private const string TEST_LOGIN = "test";
        private const string TEST_PASSWORD = "test";
    }
}
