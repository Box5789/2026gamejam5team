using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameJam.Gameplay.Spreading.Tests
{
    public sealed class SpreadBrushSelectorTests
    {
        [Test]
        public void SelectIndex_WhenIndexExists_ChangesSelectedBrush()
        {
            var selector = new SpreadBrushSelector(0);

            bool selected = selector.SelectIndex(2, 1);

            Assert.That(selected, Is.True);
            Assert.That(selector.SelectedIndex, Is.EqualTo(1));
        }

        [Test]
        public void SelectId_WhenIdExists_ChangesSelectedBrush()
        {
            var selector = new SpreadBrushSelector(0);
            var brushes = new List<SpreadBrushDefinition>
            {
                new SpreadBrushDefinition("white-rice", "White Rice", null, Color.white),
                new SpreadBrushDefinition("seasoned-rice", "Seasoned Rice", null, Color.yellow)
            };

            bool selected = selector.SelectId(brushes, "seasoned-rice");

            Assert.That(selected, Is.True);
            Assert.That(selector.SelectedIndex, Is.EqualTo(1));
        }

        [Test]
        public void ClampToCount_WhenSelectedIndexIsTooLarge_UsesLastBrush()
        {
            var selector = new SpreadBrushSelector(4);

            selector.ClampToCount(2);

            Assert.That(selector.SelectedIndex, Is.EqualTo(1));
        }
    }
}
