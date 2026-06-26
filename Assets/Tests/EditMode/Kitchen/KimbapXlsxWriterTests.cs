using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KimbapGame.Data;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KimbapXlsxWriterTests
    {
        [Test]
        public void Write_CreatesWorkbookZipWithSheetXml()
        {
            string path = Path.Combine(Application.temporaryCachePath, $"kimbap-results-{Guid.NewGuid():N}.xlsx");
            var prepared = new PreparedKimbapData();
            prepared.seaweeds.Add(new PreparedKimbapItem(IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, "plain-seaweed", "기본김"));
            prepared.riceItems.Add(new PreparedKimbapItem(IngredientType.Rice, KitchenIngredientCategory.Rice, "white-rice", "흰밥"));
            prepared.fillings.Add(new PreparedKimbapItem(IngredientType.Ham, KitchenIngredientCategory.Filling, "ham", "햄"));

            var order = new OrderData
            {
                orderId = 7,
                orderName = "테스트김밥",
                customerDialogue = "테스트 주문"
            };
            var row = new KimbapResultRow("session", DateTime.UtcNow, order, "Seaweed>Rice>Filling", prepared);

            KimbapXlsxWriter.Write(path, row);

            Assert.IsTrue(File.Exists(path));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.IsNotNull(archive.GetEntry("[Content_Types].xml"));
                ZipArchiveEntry sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
                Assert.IsNotNull(sheet);
                using (StreamReader reader = new StreamReader(sheet.Open()))
                {
                    string xml = reader.ReadToEnd();
                    Assert.IsTrue(xml.Contains("SessionId"));
                    Assert.IsTrue(xml.Contains("ScoreIgnoredP"));
                    Assert.IsTrue(xml.Contains("plain-seaweed"));
                    Assert.AreEqual(16, KimbapXlsxWriter.Headers.Length);
                }
            }

            File.Delete(path);
        }
    }
}
