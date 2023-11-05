using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lucky.Db
{
    /// <summary>
    /// Serialize/deserialize from a csv file to C# objects.
    /// </summary>
    public static class CsvHelper<T> where T : class, new()
    {
        private static readonly List<Tuple<FieldInfo, string>> s_fields;

        private class TypeComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                if (x == y)
                {
                    return 0;
                }
                if (x.IsAssignableFrom(y))
                {
                    return -1;
                }
                return 1;
            }
        }

        static CsvHelper()
        {
            // List fields
            var fields = typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                .OrderBy(f => f.DeclaringType, new TypeComparer())
                .Where(f => f.GetCustomAttributes(typeof(CsvAttribute), false) != null)
                .ToArray();
            Header = string.Join(",", fields.Select(fi => fi.Name));
            s_fields = fields.Select(fi => Tuple.Create(fi, fi.GetCustomAttribute<CsvAttribute>().Format)).ToList();
        }

        private static string Header { get; set; }

        private static string ToCsv(T value)
        {
            return string.Join(",", s_fields.Select(fi =>
            {
                var format = fi.Item2 != null ? ("{0:" + fi.Item2 + "}") : "{0}";
                return string.Format(CultureInfo.InvariantCulture, format, fi.Item1.GetValue(value));
            }));
        }

        public static int[] ParseHeader(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split(',');
                return parts.Select(name => s_fields.FindIndex(t => t.Item1.Name == name)).ToArray();
            }
            else
            {
                return null;
            }
        }

        public static T ParseLine(string line, int[] header)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split(',');
                if (parts.Length != header.Length)
                {
                    return null;
                }

                // Special case: duplicate header
                if (parts.Zip(header, (a1, a2) => Tuple.Create(a1, a2)).All(t => t.Item2 < 0 || t.Item1 == s_fields[t.Item2].Item1.Name))
                {
                    return null;
                }

                T ret = new T();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (header[i] >= 0)
                    {
                        var t = s_fields[header[i]];
                        object value = null;
                        switch (Type.GetTypeCode(t.Item1.FieldType)) 
                        {
                            case TypeCode.UInt16:
                                ushort u;
                                if (ushort.TryParse(parts[i], CultureInfo.InvariantCulture, out u))
                                {
                                    value = u;
                                }
                                break;
                            case TypeCode.Int32:
                                int r;
                                if (int.TryParse(parts[i], CultureInfo.InvariantCulture, out r))
                                {
                                    value = r;
                                }
                                break;
                            case TypeCode.Double:
                                double d;
                                if (double.TryParse(parts[i], CultureInfo.InvariantCulture, out d))
                                {
                                    value = d;
                                }
                                break;
                            case TypeCode.String:
                                value = parts[i];
                                break;
                        }

                        if (t.Item1.FieldType == typeof(DateTime))
                        {
                            DateTime dt;
                            if (DateTime.TryParseExact(parts[i], t.Item2, null, DateTimeStyles.None, out dt))
                            {
                                value = dt;
                            }
                        }
                        if (t.Item1.FieldType == typeof(TimeSpan))
                        {
                            TimeSpan ts;
                            if (TimeSpan.TryParseExact(parts[i], t.Item2, null, out ts))
                            {
                                value = ts;
                            }
                        }

                        if (value != null)
                        {
                            t.Item1.SetValue(ret, value);
                        }
                    }
                }
                return ret;
            }
            else
            {
                return null;
            }
        }

        private static void WriteCsvLine(FileInfo file, string line)
        {
            using (var stream = file.Open(FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static void WriteCsvHeader(FileInfo file)
        {
            WriteCsvLine(file, Header);
        }

        public static void WriteCsvLine(FileInfo file, T data)
        {
            WriteCsvLine(file, ToCsv(data));
        }

        public static IEnumerable<T> ReadCsv(FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                using (var reader = new StreamReader(stream))
                {
                    // Read header
                    var header = ParseHeader(reader.ReadLine());
                    // Read data
                    string line;
                    List<T> data = new List<T>();
                    while ((line = reader.ReadLine()) != null)
                    {
                        var l = ParseLine(line, header);
                        if (l != null)
                        {
                            data.Add(l);
                        }
                    }

                    return data;
                }
            }
        }
    }
}
