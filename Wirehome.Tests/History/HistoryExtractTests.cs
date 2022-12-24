using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wirehome.Tests.History
{
    [TestClass]
    public class HistoryExtractTests
    {
        //[TestMethod]
        //public async Task HistoryExtract_Build_Text_Based_Extract()
        //{
        //    var repo = HistoryRepositoryTests.CreateRepository();
        //    repo.ComponentStatusOutdatedTimeout = TimeSpan.FromHours(2);
        //    try
        //    {
        //        var startDateTime = DateTime.UtcNow;

        //        var componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime);
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime.AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        var entities = await repo.GetComponentStatusValuesAsync("c1", "s1", 1000, CancellationToken.None);
        //        Assert.AreEqual(6, entities.Count);

        //        var extract = await new HistoryExtractBuilder(repo).BuildAsync(
        //            "c1", 
        //            "s1", 
        //            startDateTime,
        //            startDateTime.AddHours(5).AddSeconds(1), 
        //            null, 
        //            HistoryExtractDataType.Text,
        //            1000,
        //            CancellationToken.None);

        //        Assert.AreEqual(6, extract.EntityCount);
        //        Assert.AreEqual(6, extract.DataPoints.Count);

        //        for (var i = 0; i < extract.DataPoints.Count; i++)
        //        {
        //            Assert.AreEqual(extract.DataPoints[i].Value, i.ToString());
        //        }
        //    }
        //    finally
        //    {
        //        repo.Delete();
        //    }
        //}

        //[TestMethod]
        //public async Task HistoryExtract_Build_Number_Based_No_Interval_Extract()
        //{
        //    var repo = HistoryRepositoryTests.CreateRepository();
        //    repo.ComponentStatusOutdatedTimeout = TimeSpan.FromHours(2);
        //    try
        //    {
        //        var startDateTime = DateTime.UtcNow;

        //        var componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime);
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("0", startDateTime.AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddHours(1).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(2).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(3).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(4).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(5).AddMinutes(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        var entities = await repo.GetComponentStatusValuesAsync("c1", "s1", 1000, CancellationToken.None);
        //        Assert.AreEqual(6, entities.Count);

        //        var extract = await new HistoryExtractBuilder(repo).BuildAsync(
        //            "c1",
        //            "s1",
        //            startDateTime,
        //            startDateTime.AddHours(5).AddSeconds(1),
        //            null,
        //            HistoryExtractDataType.Number,
        //            1000,
        //            CancellationToken.None);

        //        Assert.AreEqual(6, extract.EntityCount);
        //        Assert.AreEqual(6, extract.DataPoints.Count);

        //        for (var i = 0; i < extract.DataPoints.Count; i++)
        //        {
        //            Assert.AreEqual(extract.DataPoints[i].Value, Convert.ToDouble(i));
        //        }
        //    }
        //    finally
        //    {
        //        repo.Delete();
        //    }
        //}

        //[TestMethod]
        //public async Task HistoryExtract_Build_Number_Based_With_Interval_Extract()
        //{
        //    var repo = HistoryRepositoryTests.CreateRepository();
        //    repo.ComponentStatusOutdatedTimeout = TimeSpan.FromHours(2);
        //    try
        //    {
        //        var startDateTime = DateTime.UtcNow;

        //        var componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime);
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("1", startDateTime.AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(1));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("2", startDateTime.AddHours(1).AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(2));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("3", startDateTime.AddHours(2).AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(3));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("4", startDateTime.AddHours(3).AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(4));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("5", startDateTime.AddHours(4).AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("6", startDateTime.AddHours(5));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);
        //        componentStatusValue = HistoryRepositoryTests.CreateComponentStatusValue("6", startDateTime.AddHours(5).AddMinutes(59).AddSeconds(59));
        //        await repo.UpdateComponentStatusValueAsync(componentStatusValue, CancellationToken.None);

        //        var entities = await repo.GetComponentStatusValuesAsync("c1", "s1", 1000, CancellationToken.None);
        //        Assert.AreEqual(6, entities.Count);

        //        var extract = await new HistoryExtractBuilder(repo).BuildAsync(
        //            "c1",
        //            "s1",
        //            startDateTime.AddSeconds(1),
        //            startDateTime.AddHours(5).AddSeconds(1),
        //            TimeSpan.FromHours(2), 
        //            HistoryExtractDataType.Number,
        //            1000,
        //            CancellationToken.None);

        //        Assert.AreEqual(6, extract.EntityCount);
        //        Assert.AreEqual(3, extract.DataPoints.Count);

        //        Assert.AreEqual(1D, extract.DataPoints[0].Value);
        //        Assert.AreEqual(2.5D, extract.DataPoints[1].Value);
        //        Assert.AreEqual(5D, extract.DataPoints[2].Value);

        //    }
        //    finally
        //    {
        //        repo.Delete();
        //    }
        //}
    }
}