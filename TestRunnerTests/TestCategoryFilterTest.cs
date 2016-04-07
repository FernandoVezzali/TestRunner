using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestRunner;

namespace TestRunnerTests
{
    [TestClass]
    public class TestCategoryFilterTest
    {
        
        [TestMethod]
        public void Categories_Should_Remain_The_Same_When_DataBase_Deploy_Was_Successfull()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(true, "TestCategory=UnitTest|TestCategory=ConfigurationTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest|TestCategory=ConfigurationTest", result);
        }

        [TestMethod]
        public void Categories_Should_Remain_The_Same_When_The_Only_Category_Is_Unit_Test()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(false, "TestCategory=UnitTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest", result);
        }

        [TestMethod]
        public void Categories_Should_Be_Only_Unit_Tests_When_DataBase_Deploy_Was_NOT_Successfull()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(false, "TestCategory=UnitTest|TestCategory=ConfigurationTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest", result);
        }

        [TestMethod]
        public void ConfigurationTest_Should_Not_Be_Allowed()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(false, "TestCategory=UnitTest|TestCategory=NewCategory|TestCategory=ConfigurationTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest|TestCategory=NewCategory", result);
        }

        [TestMethod]
        public void IntegrationTest_Should_Not_Be_Allowed()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(false, "TestCategory=UnitTest|TestCategory=NewCategory|TestCategory=IntegrationTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest|TestCategory=NewCategory", result);
        }

        [TestMethod]
        public void Configuration_And_IntegrationTest_Should_Not_Be_Allowed()
        {
            // Arrange
            var categoryFilter = new TestCategoryFilter(false, "TestCategory=UnitTest|TestCategory=NewCategory|TestCategory=IntegrationTest|TestCategory=ConfigurationTest");

            // Act
            var result = categoryFilter.GetFilteredCategories();

            // Assert
            Assert.AreEqual("TestCategory=UnitTest|TestCategory=NewCategory", result);
        }
    }
}
