using System.Collections.Generic;
using KimbapGame.Data;
using NUnit.Framework;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class CsvTableParserTests
    {
        [Test]
        public void Parse_HandlesQuotedCommaAndEscapedQuotes()
        {
            List<List<string>> rows = CsvTableParser.Parse("Index,이름,분류\nr1,\"참치, 마요\",속\nr2,\"두부 \"\"스테이크\"\"\",속\n");

            Assert.AreEqual("참치, 마요", rows[1][1]);
            Assert.AreEqual("두부 \"스테이크\"", rows[2][1]);
        }

        [Test]
        public void IsEmptyRow_DetectsBlankRows()
        {
            List<List<string>> rows = CsvTableParser.Parse("Index,이름,분류\n,,\nr1,기본 김,김\n");

            Assert.IsTrue(CsvTableParser.IsEmptyRow(rows[1]));
            Assert.IsFalse(CsvTableParser.IsEmptyRow(rows[2]));
        }

        [Test]
        public void HeaderLookup_NormalizesKoreanHeaders()
        {
            List<List<string>> rows = CsvTableParser.Parse("Index, 필수 여부 ,주문-대사\nr1,O,\"안녕, 손님\"\n");
            Dictionary<string, int> header = CsvTableParser.BuildHeader(rows[0]);

            Assert.AreEqual("O", CsvTableParser.GetAny(rows[1], header, "필수여부"));
            Assert.AreEqual("안녕, 손님", CsvTableParser.GetAny(rows[1], header, "주문대사"));
        }
    }
}
