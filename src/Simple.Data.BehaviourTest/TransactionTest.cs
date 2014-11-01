﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Simple.Data.Ado;
using Simple.Data.Mocking.Ado;

namespace Simple.Data.IntegrationTest
{
    using Xunit;
    using Assert = NUnit.Framework.Assert;

    public class TransactionTest
    {
        static Database CreateDatabase(MockDatabase mockDatabase)
        {
            var mockSchemaProvider = new MockSchemaProvider();
            mockSchemaProvider.SetTables(new[] { "dbo", "Users", "BASE TABLE" });
            mockSchemaProvider.SetColumns(new[] { "dbo", "Users", "Id" },
                                          new[] { "dbo", "Users", "Name" },
                                          new[] { "dbo", "Users", "Password" },
                                          new[] { "dbo", "Users", "Age" });
            mockSchemaProvider.SetPrimaryKeys(new object[] {"dbo", "Users", "Id", 0});
            return new Database(new AdoAdapter(new MockConnectionProvider(new MockDbConnection(mockDatabase), mockSchemaProvider)));
        }

        private const string usersColumns = "[dbo].[Users].[Id],[dbo].[Users].[Name],[dbo].[Users].[Password],[dbo].[Users].[Age]";

        [Fact]
        public async void TestFindEqualWithInt32()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Find(database.Users.Id == 1);
            }
            Assert.AreEqual(("select " + usersColumns + " from [dbo].[users] where [dbo].[users].[id] = @p1").ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual(1, mockDatabase.Parameters[0]);
        }

        [Fact]
        public async void TestFindByDynamicSingleColumn()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.FindByName("Foo");
            }
            Assert.AreEqual(("select " + usersColumns + " from [dbo].[Users] where [dbo].[Users].[name] = @p1").ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Foo", mockDatabase.Parameters[0]);
        }

        [Fact]
        public async void TestInsertWithNamedArguments()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Insert(Name: "Steve", Age: 50);
                transaction.Commit();
            }
            Assert.AreEqual("insert into [dbo].[Users] ([Name],[Age]) values (@p0,@p1)".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(50, mockDatabase.Parameters[1]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestUpdateWithNamedArguments()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.UpdateById(Id: 1, Name: "Steve", Age: 50);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Age] = @p2 where [dbo].[Users].[Id] = @p3".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(50, mockDatabase.Parameters[1]);
            Assert.AreEqual(1, mockDatabase.Parameters[2]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestUpdateWithDynamicObject()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            dynamic record = new SimpleRecord();
            record.Id = 1;
            record.Name = "Steve";
            record.Age = 50;
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Update(record);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Age] = @p2 where [dbo].[Users].[Id] = @p3".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(50, mockDatabase.Parameters[1]);
            Assert.AreEqual(1, mockDatabase.Parameters[2]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestUpdateByWithDynamicObject()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            dynamic record = new SimpleRecord();
            record.Id = 1;
            record.Name = "Steve";
            record.Age = 50;
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.UpdateById(record);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Age] = @p2 where [dbo].[Users].[Id] = @p3".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(50, mockDatabase.Parameters[1]);
            Assert.AreEqual(1, mockDatabase.Parameters[2]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestUpdateWithStaticObject()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            var user = new User
                           {
                               Id = 1,
                               Name = "Steve",
                               Age = 50
                           };
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Update(user);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Password] = @p2, [Age] = @p3 where [dbo].[Users].[Id] = @p4".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(DBNull.Value, mockDatabase.Parameters[1]);
            Assert.AreEqual(50, mockDatabase.Parameters[2]);
            Assert.AreEqual(1, mockDatabase.Parameters[3]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }
        
        [Fact]
        public async void TestBulkUpdateWithStaticObject()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            var user = new User
                           {
                               Id = 1,
                               Name = "Steve",
                               Age = 50
                           };
            var users = new[] {user};
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Update(users);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Password] = @p2, [Age] = @p3 where [dbo].[Users].[Id] = @p4".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(DBNull.Value, mockDatabase.Parameters[1]);
            Assert.AreEqual(50, mockDatabase.Parameters[2]);
            Assert.AreEqual(1, mockDatabase.Parameters[3]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestUpdateByWithStaticObject()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            var user = new User
                           {
                               Id = 1,
                               Name = "Steve",
                               Age = 50
                           };
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.UpdateById(user);
                transaction.Commit();
            }
            Assert.AreEqual("update [dbo].[Users] set [Name] = @p1, [Password] = @p2, [Age] = @p3 where [dbo].[Users].[Id] = @p4".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual("Steve", mockDatabase.Parameters[0]);
            Assert.AreEqual(DBNull.Value, mockDatabase.Parameters[1]);
            Assert.AreEqual(50, mockDatabase.Parameters[2]);
            Assert.AreEqual(1, mockDatabase.Parameters[3]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestDeleteWithNamedArguments()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.Delete(Id: 1);
                transaction.Commit();
            }
            Assert.AreEqual("delete from [dbo].[Users] where [dbo].[Users].[Id] = @p1".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual(1, mockDatabase.Parameters[0]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }

        [Fact]
        public async void TestDeleteBy()
        {
            var mockDatabase = new MockDatabase();
            dynamic database = CreateDatabase(mockDatabase);
            using (var transaction = database.BeginTransaction())
            {
                await transaction.Users.DeleteById(1);
                transaction.Commit();
            }
            Assert.AreEqual("delete from [dbo].[Users] where [dbo].[Users].[Id] = @p1".ToLowerInvariant(), mockDatabase.Sql.ToLowerInvariant());
            Assert.AreEqual(1, mockDatabase.Parameters[0]);
            Assert.IsTrue(MockDbTransaction.CommitCalled);
        }
    }
}