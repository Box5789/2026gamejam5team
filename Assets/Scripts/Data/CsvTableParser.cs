using System.Collections.Generic;
using System.Text;

namespace KimbapGame.Data
{
    public static class CsvTableParser
    {
        public static List<List<string>> Parse(string csv)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;
            string source = csv ?? string.Empty;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < source.Length && source[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == '\r')
                    {
                    }
                    else if (c == '\n')
                    {
                        row.Add(field.ToString());
                        rows.Add(row);
                        row = new List<string>();
                        field.Clear();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        public static Dictionary<string, int> BuildHeader(IReadOnlyList<string> row)
        {
            Dictionary<string, int> header = new Dictionary<string, int>();
            if (row == null)
            {
                return header;
            }

            for (int i = 0; i < row.Count; i++)
            {
                string key = Normalize(row[i]);
                if (!header.ContainsKey(key))
                {
                    header.Add(key, i);
                }
            }

            return header;
        }

        public static string GetAny(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> header, params string[] columnNames)
        {
            if (row == null || header == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (header.TryGetValue(Normalize(columnNames[i]), out int index) && index >= 0 && index < row.Count)
                {
                    return row[index].Trim();
                }
            }

            return string.Empty;
        }

        public static bool IsEmptyRow(IReadOnlyList<string> row)
        {
            if (row == null)
            {
                return true;
            }

            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .TrimStart('\ufeff')
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .ToLowerInvariant();
        }
    }
}
