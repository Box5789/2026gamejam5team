using System.Linq;
using GameJam.Gameplay.Spreading;
using KimbapGame.Data;
using KimbapGame.Kitchen;
using NUnit.Framework;
using UnityEngine;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenIngredientCatalogTests
    {
        [Test]
        public void Parse_MapsSheetCategories()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "r1,기본 김,김,500,seaweed,O\n"
                + "r16,흰쌀밥,밥,400,rice,true\n"
                + "r31,햄,속,600,ham,yes\n"
                + "r86,마요네즈,소스,100,mayo,\n");

            Assert.AreEqual(KitchenIngredientCategory.Seaweed, catalog.Items[0].Category);
            Assert.AreEqual(KitchenIngredientCategory.Rice, catalog.Items[1].Category);
            Assert.AreEqual(KitchenIngredientCategory.Filling, catalog.Items[2].Category);
            Assert.AreEqual(KitchenIngredientCategory.Sauce, catalog.Items[3].Category);
            Assert.AreEqual(500, catalog.Items[0].Price);
            Assert.IsTrue(catalog.Items[0].IsRequired);
        }

        [Test]
        public void Parse_MissingOptionalColumns_UsesDefaults()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse("Index,이름,분류\nr31,햄,속\n");

            Assert.AreEqual(1, catalog.Items.Count);
            Assert.AreEqual(0, catalog.Items[0].Price);
            Assert.AreEqual(string.Empty, catalog.Items[0].ImageName);
            Assert.IsFalse(catalog.Items[0].IsRequired);
        }

        [Test]
        public void SheetRows_ConvertToKitchenDefinitions()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "r1,기본 김,김,,,\n"
                + "r16,흰쌀밥,밥,,,\n"
                + "r31,햄,속,,,\n"
                + "r33,오이,속,,,\n");

            KitchenIngredientDefinition seaweed = catalog.Items[0].ToDefinition(null, Color.white);
            KitchenIngredientDefinition rice = catalog.Items[1].ToDefinition(null, Color.white);
            KitchenIngredientDefinition ham = catalog.Items[2].ToDefinition(null, Color.white);
            KitchenIngredientDefinition cucumber = catalog.Items[3].ToDefinition(null, Color.white);

            Assert.AreEqual("r1", seaweed.VariantId);
            Assert.AreEqual("기본 김", seaweed.DisplayName);
            Assert.AreEqual(IngredientType.Seaweed, seaweed.IngredientType);
            Assert.AreEqual(IngredientType.Rice, rice.IngredientType);
            Assert.AreEqual("white-rice", rice.RiceBrushId);
            Assert.IsNotNull(rice.RiceBrushDefinition);
            Assert.AreEqual(IngredientType.Ham, ham.IngredientType);
            Assert.AreEqual(IngredientType.GenericFilling, cucumber.IngredientType);
        }

        [Test]
        public void SheetRows_ConvertRiceBrushColumnsToBrushDefinition()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse(
                "Index,이름,분류,가격,이미지,필수여부,브러시ID,브러시이미지,브러시반경,입자수,흩뿌림반경,최소크기,최대크기,스탬프간격\n"
                + "r17,현미밥,밥,,,,brown-rice,Kitchen/Brushes/brown-rice,0.24,7,0.31,0.8,1.4,0.05\n");

            Texture2D testTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            string requestedPath = string.Empty;
            KitchenIngredientDefinition rice = catalog.Items[0].ToDefinition(
                null,
                Color.yellow,
                path =>
                {
                    requestedPath = path;
                    return testTexture;
                });
            SpreadBrushDefinition brush = rice.RiceBrushDefinition;

            Assert.IsNotNull(brush);
            Assert.AreEqual("Kitchen/Brushes/brown-rice", catalog.Items[0].BrushImagePath);
            Assert.AreEqual("Kitchen/Brushes/brown-rice", requestedPath);
            Assert.AreEqual("brown-rice", rice.RiceBrushId);
            Assert.AreEqual("brown-rice", brush.Id);
            Assert.AreEqual("현미밥", brush.DisplayName);
            Assert.AreSame(testTexture, brush.BrushTexture);
            AssertColor(Color.white, brush.Tint);
            AssertColor(Color.yellow, rice.PlaceholderColor);
            Assert.AreEqual(0.24f, brush.BrushRadius, 0.0001f);
            Assert.AreEqual(7, brush.ParticlesPerSample);
            Assert.AreEqual(0.31f, brush.ScatterRadius, 0.0001f);
            Assert.AreEqual(0.8f, brush.MinScale, 0.0001f);
            Assert.AreEqual(1.4f, brush.MaxScale, 0.0001f);
            Assert.AreEqual(0.05f, brush.StampSpacing, 0.0001f);
        }

        [Test]
        public void SheetRows_BlankRiceBrushColumnsUseBrushDefaults()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse(
                "Index,이름,분류,브러시ID,브러시이미지,브러시반경,입자수,흩뿌림반경,최소크기,최대크기,스탬프간격\n"
                + "r16,흰쌀밥,밥,,,,,,,,\n");

            KitchenIngredientDefinition rice = catalog.Items[0].ToDefinition(
                null,
                Color.yellow,
                path =>
                {
                    Assert.Fail($"Brush texture resolver should not be called for a blank path: {path}");
                    return null;
                });
            SpreadBrushDefinition brush = rice.RiceBrushDefinition;

            Assert.IsNotNull(brush);
            Assert.AreEqual("white-rice", brush.Id);
            Assert.IsNull(brush.BrushTexture);
            AssertColor(Color.yellow, brush.Tint);
            Assert.AreEqual(SpreadBrushDefinition.DefaultBrushRadius, brush.BrushRadius);
            Assert.AreEqual(SpreadBrushDefinition.DefaultParticlesPerSample, brush.ParticlesPerSample);
            Assert.AreEqual(SpreadBrushDefinition.DefaultScatterRadius, brush.ScatterRadius);
            Assert.AreEqual(SpreadBrushDefinition.DefaultMinScale, brush.MinScale);
            Assert.AreEqual(SpreadBrushDefinition.DefaultMaxScale, brush.MaxScale);
            Assert.AreEqual(SpreadBrushDefinition.DefaultStampSpacing, brush.StampSpacing);
        }

        [Test]
        public void KitchenRiceBrushTextureLoader_NormalizesResourcesPaths()
        {
            Assert.AreEqual(
                "Kitchen/Brushes/white-rice",
                KitchenRiceBrushTextureLoader.NormalizeResourcePath("Assets/Resources/Kitchen/Brushes/white-rice.png"));
        }

        [Test]
        public void KitchenRiceBrushTextureLoader_MissingPathReturnsNull()
        {
            Texture2D texture = KitchenRiceBrushTextureLoader.Load("Kitchen/Brushes/missing-rice");

            Assert.IsNull(texture);
        }

        [Test]
        public void SourceItems_ExcludeSauceRows()
        {
            KitchenIngredientCatalog catalog = KitchenIngredientCatalog.Parse(
                "Index,이름,분류,가격,이미지,필수여부\n"
                + "r31,햄,속,,,\n"
                + "r86,마요네즈,소스,,,\n");

            Assert.AreEqual(2, catalog.Items.Count);
            Assert.AreEqual(1, catalog.SourceItems.Count());
            Assert.AreEqual("햄", catalog.SourceItems.Single().DisplayName);
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.0001f);
            Assert.AreEqual(expected.g, actual.g, 0.0001f);
            Assert.AreEqual(expected.b, actual.b, 0.0001f);
            Assert.AreEqual(expected.a, actual.a, 0.0001f);
        }
    }
}
