using System;

namespace Parse.Api.Tests
{
    public class ParseUnitTest4 : IParseObject
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ObjectId { get; set; }

        public byte SomeByte { get; set; }
        public short SomeShort { get; set; }
        public int SomeInt { get; set; }
        public long SomeLong { get; set; }
        public bool? SomeNullableBool { get; set; }
        public ParseGeoPoint SomeGeoPoint { get; set; }
        public DateTime SomeDate { get; set; }
        public byte[] SomeBytes { get; set; }
        public User SomePointer { get; set; }
        public object SomeObject { get; set; }
        public int[] SomeArray { get; set; }
    }

    public class ParseUnitTest5 : IParseObject
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ObjectId { get; set; }

        public byte SomeByte { get; set; }
        public short SomeShort { get; set; }
        public int SomeInt { get; set; }
        public long SomeLong { get; set; }
        public bool? SomeNullableBool { get; set; }
        public ParseGeoPoint SomeGeoPoint { get; set; }
        public DateTime SomeDate { get; set; }
        public byte[] SomeBytes { get; set; }
        public User SomePointer { get; set; }
        public object SomeObject { get; set; }
        public int[] SomeArray { get; set; }
    }
}