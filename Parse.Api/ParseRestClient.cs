using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using RestSharp;

namespace Parse.Api
{
    public class ParseRestClient
    {
        private readonly RestClient _client;

        public ParseRestClient(string appId, string restApiKey, int timeoutMs = 0)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(restApiKey))
            {
                throw new ArgumentNullException();
            }

            _client = new RestClient
            {
                BaseUrl = ParseUrls.BASE,
                Timeout = timeoutMs,
            };
            _client.AddDefaultHeader(ParseHeaders.APP_ID, appId);
            _client.AddDefaultHeader(ParseHeaders.REST_API_KEY, restApiKey);

            // use Newtonsoft to deserialize so we can use custom converters
            _client.AddHandler("application/json", new ParseJsonDeserializer {DateFormat = ParseDate.DATE_FMT});
        }

        #region objects

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="obj">The object to be created on the server</param>
        /// <returns>A fully populated ParseObject, including ObjectId</returns>
        public T CreateObject<T>(T obj) where T : class, IParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var request = CreateRequest(ParseUrls.CLASS, Method.POST);
            request.AddUrlSegments(new {typeof (T).Name});
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request, HttpStatusCode.Created);
            obj.CreatedAt = response.CreatedAt;
            obj.UpdatedAt = response.CreatedAt; // UpdatedAt doesn't come back for create requests
            obj.ObjectId = response.ObjectId;
            return obj;
        }

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="obj">The object being updated</param>
        public T Update<T>(T obj) where T : class, IParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var request = CreateRequest(ParseUrls.CLASS_OBJECT, Method.PUT);
            request.AddUrlSegments(new {typeof (T).Name, obj.ObjectId});
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request);
            obj.UpdatedAt = response.UpdatedAt; // only UpdatedAt comes back
            return obj;
        }

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="objectId">The ObjectId of the object</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <returns>A dictionary with the object's attributes</returns>
        public T GetObject<T>(string objectId, bool includeReferences = false) where T : class, IParseObject, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var request = CreateRequest(ParseUrls.CLASS_OBJECT, Method.GET);
            request.AddUrlSegments(new { typeof(T).Name, ObjectId = objectId });

            if (includeReferences)
            {
                var pointers = typeof (T).GetProperties()
                                         .Where(x => typeof (IParseObject).IsAssignableFrom(x.PropertyType))
                                         .Select(x => x.Name).ToList();
                if (pointers.Count > 0)
                {
                    request.AddParameter("include", string.Join(",", pointers));
                }
            }

            return ExecuteAndValidate<T>(request);
        }

        /// <summary>
        /// Search for objects on Parse based on attributes. 
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="limit">The maximum number of results to be returned (Default 100)</param>
        /// <param name="skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of Dictionaries containing the objects</returns>
        public QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : class, IParseObject, new()
        {
            var request = CreateRequest(ParseUrls.CLASS, Method.GET);
            request.AddUrlSegments(new {typeof (T).Name});
            request.AddUrlParameters(new { limit, skip, count = 1 });
            if (where != null)
            {
                // use Newtonsoft so Criteria are serialized correctly
                request.AddParameter("where", JsonConvert.SerializeObject(where));
            }
            if (order != null)
            {
                request.AddParameter("order", order);
            }
            
            return ExecuteAndValidate<QueryResult<T>>(request);
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        public void DeleteObject<T>(T obj) where T : class, IParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            DeleteObject<T>(obj.ObjectId);
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="objectId">The object id to be deleted</param>
        public void DeleteObject<T>(string objectId) where T : class, IParseObject, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var request = CreateRequest(ParseUrls.CLASS_OBJECT, Method.DELETE);
            request.AddUrlSegments(new {typeof (T).Name, ObjectId = objectId});

            ExecuteAndValidate(request);
        }

        #endregion

        #region relations

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        public void AddToRelation<T>(T fromObj, string relationName, IEnumerable<IParseObject> toObjs) where T : class, IParseObject, new()
        {
            var request = CreateRequest(ParseUrls.CLASS_OBJECT, Method.PUT);
            request.AddUrlSegments(new { fromObj.ObjectId, typeof(T).Name });
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "AddRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        /// <summary>
        /// Removes from an existing relation.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        public void RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<IParseObject> toObjs) where T : class, IParseObject, new()
        {
            var request = CreateRequest(ParseUrls.CLASS_OBJECT, Method.PUT);
            request.AddUrlSegments(new { fromObj.ObjectId, typeof(T).Name });
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "RemoveRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        #endregion

        #region users

        public UserSession<T> SignUp<T>(T user) where T : User
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("Username and Password are required.");
            }

            var request = CreateRequest(ParseUrls.USER, Method.POST);
            request.AddParseBody(user);

            var response = ExecuteAndValidate<UserSessionResponse>(request, HttpStatusCode.Created);
            user.CreatedAt = response.CreatedAt;
            user.UpdatedAt = response.CreatedAt; // UpdatedAt doesn't come back on create requests
            user.ObjectId = response.ObjectId;
            return new UserSession<T>
            {
                User = user,
                SessionToken = response.SessionToken,
            };
        }

        public UserSession<T> LogIn<T>(T user) where T : User, new()
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("Username and Password are required.");
            }

            var request = CreateRequest(ParseUrls.LOGIN, Method.GET);
            request.AddUrlParameters(new { user.Username, user.Password });

            var response = ExecuteAndValidate(request);

            // TODO this is ugly
            var session = JsonConvert.DeserializeObject<UserSessionResponse>(response);
            var deserUser = JsonConvert.DeserializeObject<T>(response);
            
            return new UserSession<T>
            {
                User = deserUser,
                SessionToken = session.SessionToken,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="sesionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        public T GetUser<T>(string objectId, string sesionToken = null) where T : User, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var request = CreateRequest(ParseUrls.USER_OBJECT, Method.GET);
            request.AddUrlSegments(new {ObjectId = objectId});
            if (sesionToken != null)
            {
                request.AddHeader(ParseHeaders.SESSION_TOKEN, sesionToken);
            }

            return ExecuteAndValidate<T>(request);
        }

        public T UpdateUser<T>(T user, string sessionToken) where T : User, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var request = CreateRequest(ParseUrls.USER_OBJECT, Method.PUT);
            request.AddUrlSegments(new {user.ObjectId});
            request.AddParseBody(user);
            request.AddHeader(ParseHeaders.SESSION_TOKEN, sessionToken);

            var response = ExecuteAndValidate<ParseObject>(request);
            user.UpdatedAt = response.UpdatedAt; // only UpdatedAt comes back
            return user;
        }

        public QueryResult<T> GetUsers<T>() where T : User, new()
        {
            throw new NotImplementedException();
        }

        public void DeleteUser<T>(T user, string sessionToken) where T : User, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }
            
            var request = CreateRequest(ParseUrls.USER_OBJECT, Method.DELETE);
            request.AddUrlSegments(new {user.ObjectId});
            request.AddHeader(ParseHeaders.SESSION_TOKEN, sessionToken);
            
            ExecuteAndValidate(request);
        }

        private class UserSessionResponse : ParseObject
        {
            public string SessionToken { get; set; }
        }

        #endregion

        #region cloud functions

        public void CloudFunction(string name, object data = null)
        {
            var request = CreateRequest(ParseUrls.FUNCTION, Method.POST);
            request.AddUrlSegments(new {name});
            if (data != null)
            {
                request.AddBody(data);
            }

            ExecuteAndValidate<object>(request);
        }

        #endregion

        #region analytics

        public void MarkAppOpened(DateTime? dateUtc = null)
        {
            var request = CreateRequest(ParseUrls.APP_OPENED, Method.POST);
            if (dateUtc.HasValue)
            {
                var body = new { at = new ParseDate(dateUtc.Value) };
                request.AddBody(body);
            }
            ExecuteAndValidate(request);
        }

        #endregion

        #region helpers

        /// <summary>
        /// Creates RestRequest for Parse REST API resource.
        /// </summary>
        private IRestRequest CreateRequest(string resource, Method method)
        {
            return new RestRequest(resource, method)
            {
                DateFormat = ParseDate.DATE_FMT,
                RequestFormat = DataFormat.Json,
            };
        }

        /// <summary>
        /// Returns true if response status is valid and there is no exception.
        /// </summary>
        private bool IsValidResponse(IRestResponse response, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            return response.ResponseStatus == ResponseStatus.Completed &&
                   response.ErrorException == null &&
                   response.ErrorMessage == null &&
                   response.StatusCode == expectedCode;
        }

        /// <summary>
        /// Executes request, validates response, returns deserialized data.
        /// </summary>
        private T ExecuteAndValidate<T>(IRestRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK) where T : new()
        {
            var response = _client.Execute<T>(request);

            // make sure request went through ok
            if (!IsValidResponse(response, expectedCode))
            {
                // when a request fails, body is JSON: {code:105,error:"invalid field name: b!ng"}
                throw new ApplicationException("Parse API failed: " + response.Content, response.ErrorException);
            }

            return response.Data;
        }

        /// <summary>
        /// Executes request, validates response, returns content.
        /// </summary>
        private string ExecuteAndValidate(IRestRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var response = _client.Execute(request);

            // make sure request went through ok
            if (!IsValidResponse(response, expectedCode))
            {
                // when a request fails, body is JSON: {code:105,error:"invalid field name: b!ng"}
                throw new ApplicationException("Parse API failed: " + response.Content, response.ErrorException);
            }

            return response.Content;            
        }

        #endregion
    }

    internal static class RestSharpExtensions
    {
        public static void AddUrlSegments(this IRestRequest request, object urlSegments)
        {
            if (urlSegments == null)
            {
                return;
            }

            foreach (var prop in urlSegments.GetType().GetProperties())
            {
                request.AddUrlSegment(prop.Name, prop.GetValue(urlSegments, null).ToString());
            }
        }

        public static void AddUrlParameters(this IRestRequest request, object urlParams)
        {
            if (urlParams == null)
            {
                return;
            }

            foreach (var prop in urlParams.GetType().GetProperties())
            {
                request.AddParameter(prop.Name, prop.GetValue(urlParams, null));
            }
        }

        public static void AddParseBody(this IRestRequest request, IParseObject body)
        {
            var propsToIgnore = new HashSet<string> {"CreatedAt", "UpdatedAt", "ObjectId"};

            var dict = new Dictionary<string, object>();

            foreach (var prop in body.GetType().GetProperties().Where(x => !propsToIgnore.Contains(x.Name)))
            {
                var value = prop.GetValue(body, null);

                if (prop.PropertyType == typeof (DateTime))
                {
                    value = new ParseDate((DateTime)value);
                }
                else if (prop.PropertyType == typeof (byte[]))
                {
                    value = new ParseBytes((byte[]) value);
                }
                else if (typeof(IParseObject).IsAssignableFrom(prop.PropertyType))
                {
                    if (value != null)
                    {
                        value = new ParsePointer((IParseObject)value);
                    }
                }
                else if (prop.PropertyType.IsGenericType && value is IList && typeof(IParseObject).IsAssignableFrom(prop.PropertyType.GetGenericArguments()[0]))
                {
                    // explicity skip relations, need to be dealt with manually
                    continue;

                    // var pointers = ((IList) value).Cast<IParseObject>().Select(x => new ParsePointer(x)).ToList();
                    // value = pointers.Count == 0 ? null : new {__op = "AddRelation", objects = pointers};
                }

                dict[prop.Name] = value;
            }

            request.AddBody(dict);
        }
    }

    internal static class ParseHeaders
    {
        public const string APP_ID = "X-Parse-Application-Id";
        public const string REST_API_KEY = "X-Parse-REST-API-Key";
        public const string SESSION_TOKEN = "X-Parse-Session-Token";
    }

    internal static class ParseUrls
    {
        public const string BASE = "https://api.parse.com/1/";

        // POST to create, GET to query
        public const string CLASS = "classes/{Name}";

        // PUT to update, GET to retreive, DELETE to delete
        public const string CLASS_OBJECT = "classes/{Name}/{ObjectId}";

        // POST to sign up, GET to query
        public const string USER = "users";

        // PUT to update, GET to retreive, DELETE to delete
        public const string USER_OBJECT = "users/{ObjectId}";

        // GET to log in
        public const string LOGIN = "login";

        // POST to request password reset
        public const string PASSWORD_RESET = "requestPasswordReset";  // TODO

        // POST to create, GET to query
        public const string ROLE = "roles";// TODO

        // PUT to update, GET to retreive, DELETE to delete
        public const string ROLE_OBJECT = "roles/{ObjectId}";// TODO

        // POST to upload
        public const string FILE = "files/{FileName}";// TODO

        // POST to track analytics
        public const string APP_OPENED = "events/AppOpened";

        // POST to send push
        public const string PUSH = "push";// TODO

        // POST to upload, GET to query
        public const string INSTALLATION = "installations";// TODO

        // PUT to update, GET to retreive, DELETE to delete
        public const string INSTALLATION_OBJECT = "installations/{ObjectId}";// TODO

        // POST to call cloud function
        public const string FUNCTION = "functions/{Name}";
    }
}
