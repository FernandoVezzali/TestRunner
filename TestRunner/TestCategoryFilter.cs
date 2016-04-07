using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestRunner
{
    public class TestCategoryFilter
    {
        private string testCategory;
        private bool deployDatabaseResult;
        private List<string> _dataBaseDependentCategories;

        public TestCategoryFilter(bool deployDatabaseResult, string testCategory)
        {
            this.testCategory = testCategory;
            this.deployDatabaseResult = deployDatabaseResult;

            _dataBaseDependentCategories = new List<string>()
            {
                "TestCategory=ConfigurationTest",
                "TestCategory=IntegrationTest"
            };
        }

        public string GetFilteredCategories()
        {
            if (deployDatabaseResult || testCategory == "TestCategory=UnitTest")
            {
                return testCategory;
            }

            var filteredCategories = FilteredCategories();
            return filteredCategories;
        }

        private string FilteredCategories()
        {
            if (testCategory.IndexOf("|") == 0)
            {
                if (_dataBaseDependentCategories.Contains(testCategory))
                {
                    return String.Empty;
                }
                else
                {
                    return testCategory;
                }
            }

            var result = String.Empty;
            string[] categories = testCategory.Split('|');
            foreach (string category in categories)
            {
                if (!_dataBaseDependentCategories.Contains(category))
                {
                    result = AppendToResultString(category, result);
                }
            }
            return result;

        }

        private string AppendToResultString(string category, string result)
        {
            if (result == String.Empty)
            {
                result = category;
            }
            else
            {
                result = result + "|" + category;
            }
            return result;
        }
    }
}
