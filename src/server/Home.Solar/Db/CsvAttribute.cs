using System;

namespace Lucky.Db
{
    /// <summary>
    /// Describe field to serialize/deserialize from a csv file
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvAttribute : Attribute
    {
        public CsvAttribute(string format = null)
        {
            Format = format;
        }

        public string Name { get; set; }

        public string Format { get; private set; }
    }
}
