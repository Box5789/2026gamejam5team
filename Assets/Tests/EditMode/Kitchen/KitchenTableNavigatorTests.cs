using KimbapGame.Kitchen;
using NUnit.Framework;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenTableNavigatorTests
    {
        [TestCase(-1, 3, 0)]
        [TestCase(0, 3, 0)]
        [TestCase(1, 3, 1)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 3, 2)]
        [TestCase(99, 3, 2)]
        [TestCase(3, 4, 3)]
        [TestCase(4, 4, 3)]
        [TestCase(99, 4, 3)]
        [TestCase(2, 0, 0)]
        public void ClampTableIndex_StaysInsideTableRange(int input, int tableCount, int expected)
        {
            Assert.AreEqual(expected, KitchenTableNavigator.ClampTableIndex(input, tableCount));
        }
    }
}
