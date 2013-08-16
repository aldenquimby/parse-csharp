using System;
using System.Linq;
using NUnit.Framework;

namespace Parse.Api.Tests
{
    [TestFixture]
    public class ParseRestClientTests
    {
        private ParseRestClient _client;
        
        [SetUp]
        public void Setup()
        {
            const string appId = ""; // put your Application ID here
            const string restApiKey = ""; // put your REST API Key here

            _client = new ParseRestClient(appId, restApiKey);
        }

        public class Test
        {
            public DateTime Hey { get; set; }
            public DateTime Dude { get; set; }
            public int Yo { get; set; }
            public byte[] Bytes { get; set; }
            public UserBase User { get; set; }
        }

        [Test]
        public void TestPointers()
        {
            var obj = new ParseUnitTest7
            {
                SomeDate = DateTime.UtcNow.AddDays(-5),
            };
            var result = _client.CreateObject(obj);
            AssertParseObjectEqual(obj, result);

            // point to a different UserBase
            result.SomePointer = new User {ObjectId = "ITxOCfOtFT"};
            _client.Update(result);
            
            // make sure get works
            var result2 = _client.GetObject<ParseUnitTest7>(result.ObjectId);
            AssertParseObjectEqual(result, result2);
            Assert.AreEqual(result.SomePointer.ObjectId, result2.SomePointer.ObjectId);

            // make sure recursive get works
            result2 = _client.GetObject<ParseUnitTest7>(result.ObjectId, true);
            AssertParseObjectEqual(result, result2);
            Assert.AreEqual(result.SomePointer.ObjectId, result2.SomePointer.ObjectId);
            Assert.AreNotEqual(result.SomePointer.CreatedAt, default(DateTime));

            // remove the pointer
            result.SomePointer = null;
            _client.Update(result);
            var result3 = _client.GetObject<ParseUnitTest7>(result.ObjectId);
            AssertParseObjectEqual(result, result3);

            // delete the obj
            _client.DeleteObject(result);
        }

        [Test]
        public void TestObjects()
        {
            var obj = new ParseUnitTest7
            {
                SomeByte = 1,
                SomeShort = 2,
                SomeInt = 3,
                SomeLong = 4,
                SomeNullableBool = null,
                SomeGeoPoint = new ParseGeoPoint(40, 40),
                SomeBytes = new byte[]{1,2,3},
                SomeDate = DateTime.UtcNow.AddDays(-10),
                SomePointer = new User { ObjectId = "FWMvHBYwmK" },
                SomeObject = new {Rando=true},
                SomeArray = new []{1,2,3},
            };

            var result = _client.CreateObject(obj);
            AssertParseObjectEqual(obj, result);

            result.SomePointer = new User {ObjectId = "ITxOCfOtFT"};
            result.SomeNullableBool = true;
            result.SomeGeoPoint = null;
            var result2 = _client.Update(result);
            AssertParseObjectEqual(result, result2);

            var result3 = _client.GetObject<ParseUnitTest7>(result2.ObjectId);
            AssertParseObjectEqual(result2, result3);

            var queryResults = _client.GetObjects<ParseUnitTest7>(new 
            {
                SomeByte = new Constraint(greaterThan:-7),
                SomeInt = new Constraint(lessThanOrEqualTo:1),
            });
            Assert.IsTrue(queryResults.Count == 0);

            _client.DeleteObject(result3);

            // after an object is deleted, it should throw a NotFound
            Assert.Throws<ApplicationException>(() => _client.GetObject<ParseUnitTest7>(result2.ObjectId));
            
            // and it should not be in any query results
            var queryResults2 = _client.GetObjects<ParseUnitTest7>();
            Assert.IsFalse(queryResults2.Results.Any(x => x.ObjectId.Equals(result.ObjectId)));
        }

        [Test]
        public void TestUsers()
        {
            var user = new User
            {
                username = "test" + new Random().Next(),
                password = new Random().Next().ToString(),
            };
            user.email = user.username + "@gmail.com";

            var session = _client.SignUp(user);
            Assert.IsNotNull(session.SessionToken);
            AssertParseObjectEqual(user, session.User);

            user.phone = new Random().Next().ToString();
            var updated = _client.UpdateUser(session.User, session.SessionToken);
            AssertParseObjectEqual(updated, session.User);

            // no auth data included
            var result2 = _client.GetUser<User>(updated.ObjectId);
            Assert.IsNull(result2.authData);

            // new session
            var newSession = _client.LogIn(user);

            _client.DeleteUser(session.User, newSession.SessionToken);
        }

        [Test]
        public void TestAnalytics()
        {
            Assert.DoesNotThrow(() => _client.MarkAppOpened());
        }

        private void AssertParseObjectEqual<T>(T obj1, T obj2) where T : ParseObject
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
    }
}
