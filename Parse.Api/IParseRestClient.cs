using System;
using System.Collections.Generic;
using Parse.Api.Models;

namespace Parse.Api
{
    /// <summary>
    /// Wrapper for the Parse REST API, found here: https://parse.com/docs/rest
    /// All methods throw an exception if Parse request fails, with message that includes Parse error.
    /// Example exception: "Parse API failed with status code 400 (Bad Request): {code:105,error:"invalid field name: b!ng"}
    /// </summary>
    public interface IParseRestClient
    {
        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="obj">The object to be created on the server</param>
        /// <returns>A fully populated ParseObject, including ObjectId</returns>
        T CreateObject<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="obj">The object being updated</param>
        T Update<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="objectId">The ObjectId of the object</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <returns>A dictionary with the object's attributes</returns>
        T GetObject<T>(string objectId, bool includeReferences = false) where T : ParseObject, new();

        /// <summary>
        /// Search for objects on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result object, and the total count of results in case the results were limited</returns>
        QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseObject, new();

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        void DeleteObject<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        void AddToRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new();

        /// <summary>
        /// Removes from an existing relation
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        void RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new();

        /// <summary>
        /// Creates a new ParseUser and session
        /// </summary>
        /// <param name="user">The user to create, requires username and password</param>
        /// <returns>Fully populated created user and a session token</returns>
        UserSession<T> SignUp<T>(T user) where T : ParseUser;
        
        /// <summary>
        /// Log in as a ParseUser to get a session
        /// </summary>
        /// <param name="user">The user to log in, requires username and password</param>
        /// <returns>Fully populated logged in user and a session token</returns>
        UserSession<T> LogIn<T>(T user) where T : ParseUser, new();

        /// <summary>
        /// Get one user identified by it's Parse ID
        /// </summary>
        /// <param name="objectId">The ObjectId of the user</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <param name="sessionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        T GetUser<T>(string objectId, string sessionToken = null, bool includeReferences = false) where T : ParseUser, new();

        /// <summary>
        /// Search for users on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result users, and the total count of results in case the results were limited</returns>
        QueryResult<T> GetUsers<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseUser, new();

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        T UpdateUser<T>(T user, string sessionToken) where T : ParseUser, new();

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to delete</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        void DeleteUser<T>(T user, string sessionToken) where T : ParseUser, new();
        
        /// <summary>
        /// Executes a pre-existing cloud function, see here for details: https://www.parse.com/docs/cloud_code_guide
        /// </summary>
        /// <param name="name">The name of the cloud code function</param>
        /// <param name="data">Data to pass to the cloud code function</param>
        /// <returns>The result of the cloud code function</returns>
        string CloudFunction(string name, object data = null);
        
        /// <summary>
        /// Records an AppOpened event for Parse analytics
        /// </summary>
        /// <param name="dateUtc">The date the app was opened, or now if not specified</param>
        void MarkAppOpened(DateTime? dateUtc = null);
    }
}