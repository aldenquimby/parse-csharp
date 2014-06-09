using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Parse.Api.Converters;
using Parse.Api.Extensions;
using Parse.Api.Models;
using Parse.Api.Models.Internal;

namespace Parse.Api
{
    /// <summary>
    /// Wrapper for the Parse REST API.
    /// </summary>
    /// <seealso cref="http://parse.com/docs/rest"/>
    public class ParseRestClient : IParseRestClient
    {
        public TimeSpan Timeout { get; set; }

        private readonly WebHeaderCollection _defaultHeaders;

        public ParseRestClient(string appId, string restApiKey)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(restApiKey))
            {
                throw new ArgumentNullException();
            }

            _defaultHeaders = new WebHeaderCollection();
            _defaultHeaders.Add(ParseHeaders.APP_ID, appId);
            _defaultHeaders.Add(ParseHeaders.REST_API_KEY, restApiKey);
            Timeout = TimeSpan.FromSeconds(30);
        }

        #region objects

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="obj">The object to be created on the server</param>
        /// <returns>A fully populated ParseObject, including ObjectId</returns>
        public ParseResult<T> CreateObject<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var resource = string.Format(ParseUrls.CLASS, typeof (T).Name);
            var request = CreateRequest("POST", resource);
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request, HttpStatusCode.Created);

            var result = new ParseResult<T> {Exception = response.Exception};

            if (response.Exception == null)
            {
                obj.CreatedAt = response.Result.CreatedAt;
                obj.UpdatedAt = response.Result.CreatedAt; // UpdatedAt doesn't come back for create requests
                obj.ObjectId = response.Result.ObjectId;
                result.Result = obj;
            }

            return result;
        }

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="obj">The object being updated</param>
        public ParseResult<T> Update<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null || string.IsNullOrEmpty(obj.ObjectId))
            {
                throw new ArgumentException("ObjectId is required.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, obj.ObjectId);
            var request = CreateRequest("PUT", resource);
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request);

            var result = new ParseResult<T> { Exception = response.Exception };

            if (response.Exception == null)
            {
                obj.UpdatedAt = response.Result.UpdatedAt; // only UpdatedAt comes back
                result.Result = obj;
            }

            return result;
        }

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="objectId">The ObjectId of the object</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <returns>A dictionary with the object's attributes</returns>
        public ParseResult<T> GetObject<T>(string objectId, bool includeReferences = false) where T : ParseObject, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var type = typeof (T);

            if (typeof (ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use GetUser() instead of GetObject() to query users.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, type.Name, objectId);

            return GetObjectInternal<T>(resource, includeReferences, null);
        }

        private ParseResult<T> GetObjectInternal<T>(string resource, bool includeReferences, string sessionToken) where T : ParseObject, new()
        {
            if (includeReferences)
            {
                var pointers = typeof(T).GetProperties()
                                         .Where(x => typeof(ParseObject).IsAssignableFrom(x.PropertyType))
                                         .Select(x => x.Name).ToArray();
                if (pointers.Length > 0)
                {
                    resource += "?include=" + string.Join(",", pointers);
                }
            }

            var request = CreateRequest("GET", resource);

            if (sessionToken != null)
            {
                request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);
            }

            return ExecuteAndValidate<T>(request);
        }

        /// <summary>
        /// Search for objects on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result object, and the total count of results in case the results were limited</returns>
        public QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseObject, new()
        {
            var type = typeof (T);

            if (typeof (ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use GetUsers() instead of GetObjects() to query users.");
            }

            var resource = string.Format(ParseUrls.CLASS, type.Name);

            return GetObjectsInternal<T>(resource, where, order, limit, skip);
        }

        private QueryResult<T> GetObjectsInternal<T>(string resource, object where, string order, int limit, int skip) where T : ParseObject, new()
        {
            resource += "?limit=" + limit + "&skip=" + skip + "&count=1";
            if (where != null)
            {
                // use Newtonsoft so Criteria are serialized correctly
                resource += "&where=" + Uri.EscapeUriString(JsonConvert.SerializeObject(where));
            }
            if (order != null)
            {
                resource += "&order=" + order;
            }

            var request = CreateRequest("GET", resource);
            var response = ExecuteAndValidate<QueryResult<T>>(request);

            if (response.Result == null)
            {
                response.Result = new QueryResult<T>();
            }

            response.Result.Exception = response.Exception;

            return response.Result;
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        public ParseResult DeleteObject<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null || string.IsNullOrEmpty(obj.ObjectId))
            {
                throw new ArgumentException("ObjectId is required.");
            }

            var type = typeof(T);

            if (typeof(ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use DeleteUser() instead of DeleteObject() to delete users.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, type.Name, obj.ObjectId);
            var request = CreateRequest("DELETE", resource);
            return ExecuteAndValidate(request);
        }

        #endregion

        #region relations

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        public ParseResult AddToRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, fromObj.ObjectId);
            var request = CreateRequest("PUT", resource);
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "AddRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            return ExecuteAndValidate(request);
        }

        /// <summary>
        /// Removes from an existing relation
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        public ParseResult RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof(T).Name, fromObj.ObjectId);
            var request = CreateRequest("PUT", resource);
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "RemoveRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            return ExecuteAndValidate(request);
        }

        #endregion

        #region users

        /// <summary>
        /// Creates a new ParseUser and session
        /// </summary>
        /// <param name="user">The user to create, requires username and password</param>
        /// <returns>Fully populated created user and a session token</returns>
        public UserResult<T> SignUp<T>(T user) where T : ParseUser
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var request = CreateRequest("POST", ParseUrls.USER);
            request.AddParseBody(user);

            var response = ExecuteAndValidate<ParseObject>(request);

            var result = new UserResult<T> { Exception = response.Exception };

            if (response.Exception == null)
            {
                result.SessionToken = JsonConvert.DeserializeObject<UserResult<T>>(response.Content).SessionToken;
                user.CreatedAt = response.Result.CreatedAt;
                user.UpdatedAt = response.Result.CreatedAt; // UpdatedAt doesn't come back for create requests
                user.ObjectId = response.Result.ObjectId;
                result.User = user;
            }

            return result;
        }

        /// <summary>
        /// Log in as a ParseUser to get a session
        /// </summary>
        /// <param name="user">The user to log in, requires username and password</param>
        /// <returns>Fully populated logged in user and a session token</returns>
        public UserResult<T> LogIn<T>(T user) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var resource = ParseUrls.LOGIN + "?username=" + user.Username + "&password=" + user.Password;
            var request = CreateRequest("GET", resource);

            var response = ExecuteAndValidate<T>(request);

            var result = new UserResult<T> {Exception = response.Exception, User = response.Result};

            if (response.Exception == null && response.Content!=null)
            {
                result.SessionToken = JsonConvert.DeserializeObject<UserResult<T>>(response.Content).SessionToken;
            }

            return result;
        }

        /// <summary>
        /// Get one user identified by it's Parse ID
        /// </summary>
        /// <param name="objectId">The ObjectId of the user</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <param name="sessionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        public ParseResult<T> GetUser<T>(string objectId, string sessionToken = null, bool includeReferences = false) where T : ParseUser, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, objectId);

            return GetObjectInternal<T>(resource, includeReferences, sessionToken);
        }

        /// <summary>
        /// Search for users on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result users, and the total count of results in case the results were limited</returns>
        public QueryResult<T> GetUsers<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseUser, new()
        {
            return GetObjectsInternal<T>(ParseUrls.USER, where, order, limit, skip);
        }

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        public ParseResult<T> UpdateUser<T>(T user, string sessionToken) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = CreateRequest("PUT", resource);
            request.AddParseBody(user);
            request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);

            var response = ExecuteAndValidate<ParseObject>(request);

            var result = new ParseResult<T> { Exception = response.Exception };

            if (response.Exception == null)
            {
                user.UpdatedAt = response.Result.UpdatedAt; // only UpdatedAt comes back
                result.Result = user;
            }

            return result;
        }

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to delete</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        public ParseResult DeleteUser<T>(T user, string sessionToken) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = CreateRequest("DELETE", resource);
            request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);
            
            return ExecuteAndValidate(request);
        }

        #endregion

        #region cloud functions

        /// <summary>
        /// Executes a pre-existing cloud function, see here for details: https://www.parse.com/docs/cloud_code_guide
        /// </summary>
        /// <param name="name">The name of the cloud code function</param>
        /// <param name="data">Data to pass to the cloud code function</param>
        /// <returns>The result of the cloud code function</returns>
        public ParseResult<string> CloudFunction(string name, object data = null)
        {
            var resource = string.Format(ParseUrls.FUNCTION, name);
            
            var request = CreateRequest("POST", resource);
            request.AddBody(data ?? new {}); // need a blank body or the API borks

            var response = ExecuteAndValidate(request);

            var result = new ParseResult<string> { Exception = response.Exception };

            if (response.Exception == null)
            {
                var cloudFunctionResponse = new {result = ""};
                result.Result = JsonConvert.DeserializeAnonymousType(response.Content, cloudFunctionResponse).result;
            }

            return result;
        }

        #endregion

        #region analytics

        /// <summary>
        /// Records an AppOpened event for Parse analytics
        /// </summary>
        /// <param name="dateUtc">The date the app was opened, or now if not specified</param>
        public ParseResult MarkAppOpened(DateTime? dateUtc = null)
        {
            var request = CreateRequest("POST", ParseUrls.APP_OPENED);
            if (dateUtc.HasValue)
            {
                request.AddBody(new {at = new ParseDate(dateUtc.Value)});
            }
            else
            {
                request.AddBody(new {}); // need a blank body or the API borks
            }
            return ExecuteAndValidate(request);
        }

        #endregion

        #region helpers

        private HttpWebRequest CreateRequest(string httpMethod, string resource)
        {
            var request = WebRequest.CreateHttp(ParseUrls.BASE + resource);
            
            // set method
            request.Method = httpMethod;

            // add defaults
            foreach (var headerKey in _defaultHeaders.AllKeys)
            {
                request.Headers.Add(headerKey, _defaultHeaders[headerKey]);
            }

            return request;
        }

        private ParseResult<T> ExecuteAndValidate<T>(WebRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var contentResult = ExecuteAndValidate(request, expectedCode);

            var result = new ParseResult<T> { Exception = contentResult.Exception };

            if (contentResult.Exception == null)
            {
                try
                {
                    result.Result = JsonConvert.DeserializeObject<T>(contentResult.Content, new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new ParseBytesConverter(), new ParseDateConverter() },
                    });
                }
                catch (Exception e)
                {
                    result.Exception = new ParseException { Error = "Parse API succeeded, but deserialization failed. " + e.Message };
                }
            }

            return result;
        }

        private ParseResult ExecuteAndValidate(WebRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var response = Execute(request, expectedCode);

            var result = new ParseResult
            {
                Content = response.Content
            };

            if (response.StatusCode != expectedCode)
            {
                // invalid status code, try to parse exception from content
                try
                {
                    result.Exception = JsonConvert.DeserializeObject<ParseException>(response.Content);
                }
                catch
                {
                    result.Exception = new ParseException { Error = "Parse API failed with unknown exception" };
                }
                result.Exception.StatusCode = response.StatusCode;
            }

            return result;
        }

        private ParseResponse Execute(WebRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var done = new ManualResetEvent(false);

            var response = new ParseResponse();

            request.BeginGetResponse(ar1 =>
            {
                try
                {
                    var theRequest = (HttpWebRequest) ar1.AsyncState;
                    HttpWebResponse httpResponse;

                    try
                    {
                        httpResponse = (HttpWebResponse) theRequest.EndGetResponse(ar1);
                    }
                    catch (WebException we)
                    {
                        // server responses in the range of 4xx and 5xx throw a WebException
                        httpResponse = (HttpWebResponse) we.Response;
                    }

                    response.StatusCode = httpResponse.StatusCode;

                    try
                    {
                        using (var stream = httpResponse.GetResponseStream())
                        using (var sr = new StreamReader(stream))
                        {
                            response.Content = sr.ReadToEnd();
                        }
                    }
                    catch
                    {
                        response.Content = null;
                    }

                    httpResponse.Dispose();
                }
                finally
                {
                    done.Set();
                }
            }, request);

            done.WaitOne(Timeout);

            return response;
        }

        internal class ParseResponse
        {
            public string Content { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }

        #endregion
    }
}
