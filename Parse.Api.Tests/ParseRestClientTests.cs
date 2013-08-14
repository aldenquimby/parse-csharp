using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using RestSharp.Deserializers;

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
            public User User { get; set; }
        }

        [Test]
        public void TestPointers()
        {

            // CUSTOM JSON DESERIALIZATOIN
            // Pointer already works 
            // GeoPoint already works 
            // Date handled
            // Bytes handled
            // TODO relation

            var test = "{" +
                       "Hey:{\"__type\":\"Date\",\"iso\":\"2011-08-21T18:02:52.249Z\"}," +
                       "Dude:\"2011-08-21T18:02:52.249Z\"," +
                       "yo:7," +
                       "Bytes:{\"__type\":\"Bytes\",\"base64\":\"AQID\"}," +
                       "User:{\"__type\":\"Pointer\",\"className\":\"_User\",\"objectId\":\"asd3AS3\"}" +
                       "}";
            var tmp = JsonConvert.DeserializeObject<Test>(test, new ParseDateConverter(), new ParseBytesConverter());

            var obj = new ParseUnitTest5
            {
                SomePointer = new User {ObjectId = "FWMvHBYwmK"},
            };


            var result = _client.CreateObject(obj);
            AssertParseObjectEqual(obj, result);

            // point to a different User
            result.SomePointer = new User {ObjectId = "ITxOCfOtFT"};
            _client.Update(result);
            var result2 = _client.GetObject<ParseUnitTest5>(result.ObjectId);
            AssertParseObjectEqual(result, result2);

            // remove the pointer
            result.SomePointer = null;
            _client.Update(result);
            var result3 = _client.GetObject<ParseUnitTest5>(result.ObjectId);
            AssertParseObjectEqual(result, result3);

            // delete the obj
            _client.DeleteObject(result);
        }

        [Test]
        public void TestObjects()
        {
            var obj = new ParseUnitTest4
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
            var result2 = _client.Update(result);
            AssertParseObjectEqual(result, result2);

            var result3 = _client.GetObject<ParseUnitTest4>(result2.ObjectId);
            AssertParseObjectEqual(result2, result3);

            var queryResults = _client.GetObjectsWithQuery<ParseUnitTest4>(new 
            {
                SomeByte = new Constraint(greaterThan:-7),
                SomeInt = new Constraint(lessThanOrEqualTo:1),
            });
            Assert.IsTrue(queryResults.Count == 0);

            _client.DeleteObject(result3);

            // after an object is deleted, it should throw a NotFound
            Assert.Throws<ApplicationException>(() => _client.GetObject<ParseUnitTest4>(result2.ObjectId));
            
            // and it should not be in any query results
            var queryResults2 = _client.GetObjectsWithQuery<ParseUnitTest4>();
            Assert.IsFalse(queryResults2.Results.Any(x => x.ObjectId.Equals(result.ObjectId)));
        }

        private void AssertParseObjectEqual<T>(T obj1, T obj2) where T : class, IParseObject
        {
            if (obj1 == null && obj2 == null)
            {
                return;
            }

            Assert.IsNotNull(obj1);
            Assert.IsNotNull(obj2);

            foreach (var prop in typeof (T).GetProperties())
            {
                var prop1 = prop.GetValue(obj1, null);
                var prop2 = prop.GetValue(obj2, null);
                Assert.AreEqual(prop1, prop2);
            }
        }
    }
}
