using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace KimbapGame.Kitchen
{
    public static class KimbapXlsxReader
    {
        public static List<KimbapResultRow> Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            if (!File.Exists(path))
            {
                return new List<KimbapResultRow>();
            }

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry == null)
                {
                    return new List<KimbapResultRow>();
                }

                using (Stream stream = sheetEntry.Open())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return ParseSheetXml(reader.ReadToEnd());
                }
            }
        }

        private static List<KimbapResultRow> ParseSheetXml(string xml)
        {
            List<KimbapResultRow> rows = new List<KimbapResultRow>();
            if (string.IsNullOrWhiteSpace(xml))
            {
                return rows;
            }

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument document = XDocument.Parse(xml);
            List<List<string>> values = document.Descendants(ns + "row")
                .Select(row => row.Elements(ns + "c")
                    .Select(cell => (string)cell.Element(ns + "is")?.Element(ns + "t") ?? string.Empty)
                    .ToList())
                .ToList();

            if (values.Count <= 1)
            {
                return rows;
            }

            Dictionary<string, int> header = BuildHeader(values[0]);
            for (int i = 1; i < values.Count; i++)
            {
                List<string> row = values[i];
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                rows.Add(new KimbapResultRow
                {
                    sessionId = Get(row, header, "SessionId"),
                    timestampUtc = ParseDate(Get(row, header, "TimestampUtc")),
                    orderId = ParseInt(Get(row, header, "OrderId")),
                    orderName = Get(row, header, "OrderName"),
                    customerDialogue = Get(row, header, "CustomerDialogue"),
                    tableSequence = Get(row, header, "TableSequence"),
                    seaweedItems = Get(row, header, "SeaweedItems"),
                    riceItems = Get(row, header, "RiceItems"),
                    fillingItems = Get(row, header, "FillingItems"),
                    allIngredients = Get(row, header, "AllIngredients")
                });
            }

            return rows;
        }

        private static Dictionary<string, int> BuildHeader(List<string> row)
        {
            Dictionary<string, int> header = new Dictionary<string, int>();
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

        private static string Get(List<string> row, Dictionary<string, int> header, string key)
        {
            return header.TryGetValue(Normalize(key), out int index) && index < row.Count ? row[index] : string.Empty;
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime result)
                ? result
                : DateTime.MinValue;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }
    }
}
