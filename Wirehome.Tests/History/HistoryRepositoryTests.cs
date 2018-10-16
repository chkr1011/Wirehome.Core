using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.History;
using Wirehome.Core.History.Repository;
using Wirehome.Core.History.Repository.Entities;

namespace Wirehome.Tests.History
{
    [TestClass]
    public class HistoryRepositoryTests
    {
        [TestMethod]
        public void ComponentStatusValue_Create_First_Entity()
        {
            var repo = CreateRepository();
            try
            {
                var componentStatusValue = CreateComponentStatusValue("0", DateTime.UtcNow);

                repo.UpdateComponentStatusValue(componentStatusValue);
                var entities = repo.GetComponentStatusValues("c1", "s1");

                Assert.AreEqual(1, entities.Count);

                var entity = entities.First();

                Assert.AreEqual(componentStatusValue.ComponentUid, entity.ComponentUid);
                Assert.AreEqual(componentStatusValue.StatusUid, entity.StatusUid);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeStart);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeEnd);
                Assert.AreEqual(componentStatusValue.Value, entity.Value);
            }
            finally 
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Update_Existing()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("0", startDateTime.AddSeconds(59));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(1, entities.Count);
                var entity = entities.First();

                Assert.AreEqual(componentStatusValue.ComponentUid, entity.ComponentUid);
                Assert.AreEqual(componentStatusValue.StatusUid, entity.StatusUid);
                AssertDateTimesAreEqual(startDateTime, entity.RangeStart);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeEnd);
                Assert.AreEqual(componentStatusValue.Value, entity.Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Create_New_Due_To_Outdated_Existing()
        {
            var repo = CreateRepository();
            repo.ComponentStatusPullInterval = TimeSpan.FromMinutes(1);
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("0", startDateTime.AddSeconds(61));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(2, entities.Count);
                var entity = entities.Skip(1).First();

                Assert.AreEqual(componentStatusValue.ComponentUid, entity.ComponentUid);
                Assert.AreEqual(componentStatusValue.StatusUid, entity.StatusUid);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeStart);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeEnd);
                Assert.AreEqual(componentStatusValue.Value, entity.Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Create_New_Due_To_Different_Value()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddSeconds(5));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(2, entities.Count);
                var entity = entities.Skip(1).First();

                Assert.AreEqual(componentStatusValue.ComponentUid, entity.ComponentUid);
                Assert.AreEqual(componentStatusValue.StatusUid, entity.StatusUid);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeStart);
                AssertDateTimesAreEqual(componentStatusValue.Timestamp, entity.RangeEnd);
                Assert.AreEqual(componentStatusValue.Value, entity.Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Assign_Previous_And_Next()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddDays(5));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1").OrderBy(e => e.RangeStart).ToList();
                Assert.AreEqual(2, entities.Count);

                Assert.IsNull(entities[0].PreviousEntityID);
                Assert.AreEqual(entities[0].NextEntityID, entities[1].ID);

                Assert.IsNull(entities[1].NextEntityID);
                Assert.AreEqual(entities[1].PreviousEntityID, entities[0].ID);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Load_Full_Range()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddHours(1));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("2", startDateTime.AddHours(2));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("3", startDateTime.AddHours(3));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("4", startDateTime.AddHours(4));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("5", startDateTime.AddHours(5));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(6, entities.Count);

                entities = repo.GetComponentStatusValues(
                    "c1",
                    "s1",
                    startDateTime, 
                    startDateTime.AddHours(5));

                Assert.AreEqual(6, entities.Count);

                Assert.AreEqual("0", entities[0].Value);
                Assert.AreEqual("1", entities[1].Value);
                Assert.AreEqual("2", entities[2].Value);
                Assert.AreEqual("3", entities[3].Value);
                Assert.AreEqual("4", entities[4].Value);
                Assert.AreEqual("5", entities[5].Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Load_Middle_Range()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddHours(1));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("2", startDateTime.AddHours(2));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("3", startDateTime.AddHours(3));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("4", startDateTime.AddHours(4));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("5", startDateTime.AddHours(5));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(6, entities.Count);

                entities = repo.GetComponentStatusValues(
                    "c1",
                    "s1",
                    startDateTime.AddHours(2),
                    startDateTime.AddHours(4));

                Assert.AreEqual(3, entities.Count);

                Assert.AreEqual("2", entities[0].Value);
                Assert.AreEqual("3", entities[1].Value);
                Assert.AreEqual("4", entities[2].Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Build_Ranges()
        {
            var repo = CreateRepository();
            repo.ComponentStatusPullInterval = TimeSpan.FromHours(2);
            try
            {
                var startDateTime = DateTime.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("0", startDateTime.AddHours(0.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddHours(1));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddHours(1.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("2", startDateTime.AddHours(2));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("2", startDateTime.AddHours(2.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("3", startDateTime.AddHours(3));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("3", startDateTime.AddHours(3.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("4", startDateTime.AddHours(4));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("4", startDateTime.AddHours(4.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("5", startDateTime.AddHours(5));
                repo.UpdateComponentStatusValue(componentStatusValue);
                componentStatusValue = CreateComponentStatusValue("5", startDateTime.AddHours(5.99));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1");
                Assert.AreEqual(6, entities.Count);

                entities = repo.GetComponentStatusValues(
                    "c1",
                    "s1",
                    startDateTime,
                    startDateTime.AddHours(5));

                Assert.AreEqual(6, entities.Count);

                Assert.AreEqual("0", entities[0].Value);
                Assert.AreEqual("1", entities[1].Value);
                Assert.AreEqual("2", entities[2].Value);
                Assert.AreEqual("3", entities[3].Value);
                Assert.AreEqual("4", entities[4].Value);
                Assert.AreEqual("5", entities[5].Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        public static ComponentStatusValue CreateComponentStatusValue(string value, DateTime timestamp, string statusUid = "s1", string componentUid = "c1")
        {
            return new ComponentStatusValue
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                Value = value,
                Timestamp = timestamp
            };
        }

        public static HistoryRepository CreateRepository()
        {
            var optionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());

            var componentHistoryRepository = new HistoryRepository();
            componentHistoryRepository.Initialize();

            return componentHistoryRepository;
        }

        private static void AssertDateTimesAreEqual(DateTime expected, DateTime actual)
        {
            var diff = (expected - actual).TotalMilliseconds;
            if (diff > 0.001)
            {
                throw new AssertFailedException("Datetimes do not match.");
            }
        }
    }
}
