using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace KimbapGame.Kitchen
{
    public static class KimbapXlsxWriter
    {
        public static readonly string[] Headers =
        {
            "SessionId",
            "TimestampUtc",
            "OrderId",
            "OrderName",
            "CustomerDialogue",
            "TableSequence",
            "SeaweedItems",
            "RiceItems",
            "FillingItems",
            "AllIngredients",
            "ScoreIgnoredK",
            "ScoreIgnoredL",
            "ScoreIgnoredM",
            "ScoreIgnoredN",
            "ScoreIgnoredO",
            "ScoreIgnoredP"
        };

        public static void Write(string path, KimbapResultRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            Write(path, new[] { row });
        }

        public static void Write(string path, IEnumerable<KimbapResultRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
                WriteEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
                WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
                WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheetXml(rows ?? Enumerable.Empty<KimbapResultRow>()));
            }
        }

        private static void WriteEntry(ZipArchive archive, string entryName, string contents)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(contents);
            }
        }

        private static string BuildContentTypesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
                + "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
                + "</Types>";
        }

        private static string BuildRootRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
                + "</Relationships>";
        }

        private static string BuildWorkbookXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" "
                + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
                + "<sheets><sheet name=\"KimbapResults\" sheetId=\"1\" r:id=\"rId1\"/></sheets>"
                + "</workbook>";
        }

        private static string BuildWorkbookRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>"
                + "</Relationships>";
        }

        private static string BuildSheetXml(IEnumerable<KimbapResultRow> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendRow(builder, 1, Headers);

            int rowIndex = 2;
            foreach (KimbapResultRow row in rows)
            {
                AppendRow(builder, rowIndex, BuildValues(row));
                rowIndex++;
            }

            builder.Append("</sheetData></worksheet>");
            return builder.ToString();
        }

        private static string[] BuildValues(KimbapResultRow row)
        {
            return new[]
            {
                row.sessionId,
                row.timestampUtc.ToString("o"),
                row.orderId == 0 ? string.Empty : row.orderId.ToString(),
                row.orderName,
                row.customerDialogue,
                row.tableSequence,
                row.seaweedItems,
                row.riceItems,
                row.fillingItems,
                row.allIngredients,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };
        }

        private static void AppendRow(StringBuilder builder, int rowIndex, IReadOnlyList<string> values)
        {
            builder.Append("<row r=\"").Append(rowIndex).Append("\">");
            for (int i = 0; i < values.Count; i++)
            {
                string cellReference = $"{GetColumnName(i + 1)}{rowIndex}";
                builder.Append("<c r=\"").Append(cellReference).Append("\" t=\"inlineStr\"><is><t>");
                builder.Append(EscapeXml(values[i] ?? string.Empty));
                builder.Append("</t></is></c>");
            }
            builder.Append("</row>");
        }

        private static string GetColumnName(int index)
        {
            var builder = new StringBuilder();
            while (index > 0)
            {
                index--;
                builder.Insert(0, (char)('A' + (index % 26)));
                index /= 26;
            }

            return builder.ToString();
        }

        private static string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
