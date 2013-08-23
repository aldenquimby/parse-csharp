using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parse.Api.Models;

namespace Parse.Api.Tests
{
    [TestFixture]
    public class ParseRestClientTests
    {
        private ParseRestClient _client;

        // fill these fields in to run all tests
        private const string APP_ID = "";
        private const string REST_API_KEY = "";
        private const string VALID_USER_ID1 = "";
        private const string VALID_USER_ID2 = "";
        private const string CLOUD_FUNCTION_NAME = "";
        private const string CLOUD_FUNCTION_RESULT = "";

        [SetUp]
        public void Setup()
        {
            _client = new ParseRestClient(APP_ID, REST_API_KEY);
        }

        [Test]
        public void TestPointers()
        {
            // setup
            var obj = GetFakeObj();
            var result = _client.CreateObject(obj).Result;

            // check creating pointer worked
            Assert.AreEqual(obj.SomePointer.ObjectId, result.SomePointer.ObjectId);

            // move the pointer
            result.SomePointer = new MyUser {ObjectId = VALID_USER_ID2};
            _client.Update(result);
            var result2 = _client.GetObject<ParseUnitTestObj>(result.ObjectId).Result;
            Assert.AreEqual(result.SomePointer.ObjectId, result2.SomePointer.ObjectId);

            // remove the pointer
            result.SomePointer = null;
            _client.Update(result);
            var result3 = _client.GetObject<ParseUnitTestObj>(result.ObjectId).Result;
            Assert.IsNull(result3.SomePointer);

            // tear down
            _client.DeleteObject(result);
        }

        [Test]
        public void TestObjects()
        {
            var obj = GetFakeObj();
            
            // make sure creating works
            var result = _client.CreateObject(obj).Result;
            AssertParseObjectEqual(obj, result);

            // make sure updating works
            result.SomeNullableBool = true;
            result.SomeGeoPoint = null;
            var result2 = _client.Update(result).Result;
            AssertParseObjectEqual(result, result2);

            // make sure retreive works
            var result3 = _client.GetObject<ParseUnitTestObj>(result2.ObjectId).Result;
            AssertParseObjectEqual(result2, result3);

            // make sure recursive retreive works
            result3 = _client.GetObject<ParseUnitTestObj>(result.ObjectId, true).Result;
            AssertParseObjectEqual(result2, result3);
            Assert.AreNotEqual(result3.SomePointer.CreatedAt, default(DateTime));

            // make sure querying works
            var result4 = _client.GetObjects<ParseUnitTestObj>(new 
            {
                SomeByte = new Constraint {GreaterThan = obj.SomeByte + 1},
                SomeInt = new Constraint {LessThanOrEqualTo = obj.SomeInt - 1},
            });
            Assert.IsTrue(result4.TotalCount == 0);
            result4 = _client.GetObjects<ParseUnitTestObj>(new
            {
                SomeShort = new Constraint{NotEqualTo = obj.SomeShort + 1},
            });
            Assert.IsTrue(result4.TotalCount > 0);

            // make sure delete works
            _client.DeleteObject(result3);
            var shouldFail = _client.GetObject<ParseUnitTestObj>(result2.ObjectId);
            Assert.IsNotNull(shouldFail.Exception);

            var result5 = _client.GetObjects<ParseUnitTestObj>();
            Assert.IsFalse(result5.Results.Any(x => x.ObjectId.Equals(result.ObjectId)));
        }

        [Test]
        public void TestUsers()
        {
            var user = GetFakeUser();

            // make sure sign up works
            var session = _client.SignUp(user);
            Assert.IsNotNull(session.SessionToken);
            AssertParseObjectEqual(user, session.User);

            // make sure update works
            user.phone = "+" + new Random().Next();
            var updated = _client.UpdateUser(session.User, session.SessionToken).Result;
            AssertParseObjectEqual(updated, session.User);

            // make sure retreive works
            var result2 = _client.GetUser<MyUser>(updated.ObjectId, session.SessionToken).Result;
            AssertParseObjectEqual(session.User, result2);

            // make sure query works
            var result3 = _client.GetUsers<MyUser>(new
            {
                email = new Constraint{In = new List<object>{user.Email}},
                phone = new Constraint{NotIn = new List<object>{user.phone + "someStuff"}},
            });
            Assert.IsTrue(result3.TotalCount > 0);

            // make sure LogIn works
            Assert.DoesNotThrow(() => _client.LogIn(user));

            // make sure delete works
            _client.DeleteUser(session.User, session.SessionToken);
            var shouldFail = _client.GetUser<MyUser>(session.User.ObjectId);
            Assert.IsNotNull(shouldFail.Exception);
        }

        [Test]
        public void TestAnalytics()
        {
            var result = _client.MarkAppOpened();
            Assert.IsNull(result.Exception);
        }

        [Test]
        public void TestCloudFunction()
        {
            var result = _client.CloudFunction(CLOUD_FUNCTION_NAME).Result;
            Assert.AreEqual(result, CLOUD_FUNCTION_RESULT);
        }

        [Test]
        public void TestRelations()
        {
            // set up
            var obj = GetFakeObj();
            obj = _client.CreateObject(obj).Result;

            var allUsers = _client.GetUsers<MyUser>().Results;

            // make sure adding works
            _client.AddToRelation(obj, "SomeRelation", allUsers);

            // make sure removing works
            _client.RemoveFromRelation(obj, "SomeRelation", new[] {allUsers.First()});

            // tear down
            _client.DeleteObject(obj);
        }

        #region helpers

        private static ParseUnitTestObj GetFakeObj()
        {
            return new ParseUnitTestObj
            {
                SomeByte = 1,
                SomeShort = 2,
                SomeInt = 3,
                SomeLong = 4,
                SomeNullableBool = null,
                SomeGeoPoint = new ParseGeoPoint(40, 40),
                SomeBytes = new byte[] { 1, 2, 3 },
                SomeDate = DateTime.UtcNow.AddDays(-10),
                SomeNullableDate = DateTime.UtcNow.AddDays(-30),
                SomePointer = new MyUser { ObjectId = VALID_USER_ID1 },
                SomeObject = new { Rando = true },
                SomeArray = new[] { 1, 2, 3 },
            };
        }

        private static MyUser GetFakeUser()
        {
            var rand = new Random().Next();
            return new MyUser
            {
                Username = "user" + rand,
                Password = "pass" + rand,
                Email = "email" + rand + "@gmail.com",
            };
        }

        private static void AssertParseObjectEqual<T>(T obj1, T obj2) where T : ParseObject
        {
            if (obj1 == null && obj2 == null)
            {
                return;
            }

            Assert.IsNotNull(obj1);
            Assert.IsNotNull(obj2);

            foreach (var prop in typeof (T).GetProperties())
            {
                if (prop.PropertyType.IsClass)
                {
                    continue;
                }

                var prop1 = prop.GetValue(obj1, null);
                var prop2 = prop.GetValue(obj2, null);

                if (prop.PropertyType == typeof (DateTime))
                {
                    var diff = ((DateTime) prop1).Subtract((DateTime) prop2);
                    Assert.IsTrue(Math.Abs(diff.TotalMilliseconds) < 1);
                }
                else
                {
                    Assert.AreEqual(prop1, prop2);
                }
            }
        }

        #endregion
    }
}
