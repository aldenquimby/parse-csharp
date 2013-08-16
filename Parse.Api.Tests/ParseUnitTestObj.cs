using System;
using Parse.Api.Models;

namespace Parse.Api.Tests
{
    public class ParseUnitTestObj : ParseObject
    {
        public byte SomeByte { get; set; }
        public short SomeShort { get; set; }
        public int SomeInt { get; set; }
        public long SomeLong { get; set; }
        public bool? SomeNullableBool { get; set; }
        public ParseGeoPoint SomeGeoPoint { get; set; }
        public DateTime SomeDate { get; set; }
        public DateTime? SomeNullableDate { get; set; }
        public byte[] SomeBytes { get; set; }
        public MyUser SomePointer { get; set; }
        public object SomeObject { get; set; }
        public int[] SomeArray { get; set; }
    }

    public class MyUser : ParseUser
    {
        public string phone { get; set; }
    }
}