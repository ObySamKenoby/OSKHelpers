using OSKHelpers.Common;
using System;
using System.Collections.Generic;

namespace OSKHelpers.Tests.Interactive.Common
{
    public class ObjectUtilsTests
    {
        #region Support classes

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime BirthDate { get; set; }
        }

        private class Order
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime OrderDate { get; set; }
        }

        private class Product
        {
            public string Name { get; set; }
            public double Price { get; set; }
            public bool Available { get; set; }
        }

        #endregion

        #region Methods

        public static void TestDump()
        {
            // List of test objects
            List<object> testObjects = new List<object>
            {
                new Person { Name = "Samuele", Age = 30, BirthDate = new DateTime(1995, 5, 27) },
                new Order { Id = 1, Amount = 150.75m, OrderDate = DateTime.UtcNow },
                new Product { Name = "Laptop", Price = 999.99, Available = true },
                "Test string",
                42,
                3.14,
                DateTime.UtcNow,
                new DateTimeOffset(DateTime.UtcNow),
                Guid.NewGuid(),
                new List<int> { 1, 2, 3, 4, 5 } // Collection test
            };

            // Test execution
            Console.WriteLine("Test ObjectUtils.Dump:");
            foreach (var obj in testObjects)
            {
                Console.WriteLine();
                ObjectUtils.Dump(obj);
            }
        }

        #endregion
    }
}
