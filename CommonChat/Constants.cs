using System;

namespace CommonChat
{
    public class Constants
    {
        public static readonly string TABLE_NAME = System.Environment.GetEnvironmentVariable("TABLE_NAME");

        public const string ConnectionIdField = "connectionId";
    }
}