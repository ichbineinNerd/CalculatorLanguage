using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calculator;

namespace CalculatorTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Basic() {
            Assert.AreEqual(Program.Run("10+10"), 20);
        }
        [TestMethod]
        public void Paranthesis() {
            Assert.AreEqual(Program.Run("10+(20*2)"), 10+(20*2));
        }
        [TestMethod]
        public void Constants() {
            Program.SetVal("ABC", 100);
            Assert.AreEqual(Program.Run("ABC+(20*2)"), 100 + (20 * 2));
        }
        [TestMethod]
        public void Functions() {
            Program.SetVal("ABC", 100);
            Program.AddMethod("DEF", (int x, int y) => x * y);
            Assert.AreEqual(Program.Run("ABC+DEF(10,10)"), 100 + 100);
        }
    }
}
