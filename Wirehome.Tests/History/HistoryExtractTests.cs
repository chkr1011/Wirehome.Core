using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.History.Extract;

namespace Wirehome.Tests.History
{
    [TestClass]
    public class HistoryExtractTests
    {
        [TestMethod]
        public void HistoryExtract_Build_Simple()
        {
            var repo = HistoryRepositoryTests.CreateRepository();
            repo.ComponentStatusOutdatedTimeout = TimeSpan.FromHours(2);
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime.AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1).AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2).AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3).AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4).AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5).AddMinutes(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(6, entities.Count);

                var extract = new HistoryExtractBuilder(repo).Build(
                    "c1", 
                    "s1", 
                    startDateTime,
                    startDateTime.AddHours(5), 
                    TimeSpan.FromMinutes(5), 
                    HistoryExtractDataType.Text);

                Assert.AreEqual(6, extract.EntityCount);
                Assert.AreEqual(61, extract.DataPoints.Count);

                var expectedValue = "0";
                var counter = 0;
                foreach (var dataPoint in extract.DataPoints)
                {
                    Assert.AreEqual(expectedValue, dataPoint.Value);

                    counter++;

                    if (counter >= 49)
                    {
                        expectedValue = "4";
                    }
                    else if (counter >= 37)
                    {
                        expectedValue = "3";
                    }
                    else if (counter >= 25)
                    {
                        expectedValue = "2";
                    }
                    else if (counter >= 13)
                    {
                        expectedValue = "1";
                    }
                }
            }
            finally
            {
                repo.Delete();
            }
        }
    }
}
