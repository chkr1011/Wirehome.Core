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
                var componentStatusValue = CreateComponentStatusValue("0", DateTimeOffset.UtcNow);

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
                var startDateTime = DateTimeOffset.UtcNow;

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
            try
            {
                var startDateTime = DateTimeOffset.UtcNow;

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
                var startDateTime = DateTimeOffset.UtcNow;

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
        public void ComponentStatusValue_Assign_Predecessor()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTimeOffset.UtcNow;

                var componentStatusValue = CreateComponentStatusValue("0", startDateTime);
                repo.UpdateComponentStatusValue(componentStatusValue);

                componentStatusValue = CreateComponentStatusValue("1", startDateTime.AddDays(5));
                repo.UpdateComponentStatusValue(componentStatusValue);

                var entities = repo.GetComponentStatusValues("c1", "s1").OrderBy(e => e.RangeStart).ToList();
                Assert.AreEqual(2, entities.Count);

                Assert.IsNull(entities[0].PredecessorID);
                Assert.AreEqual(entities[1].PredecessorID, entities[0].ID);
            }
            finally
            {
                repo.Delete();
            }
        }

        [TestMethod]
        public void ComponentStatusValue_Load_Range()
        {
            var repo = CreateRepository();
            try
            {
                var startDateTime = DateTimeOffset.UtcNow;

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

                entities = repo.GetComponentStatusValues("c1", "s1", startDateTime.AddMinutes(61), startDateTime.AddHours(2.99));
                Assert.AreEqual(3, entities.Count);

                Assert.AreEqual("1", entities[0].Value);
                Assert.AreEqual("3", entities[2].Value);
            }
            finally
            {
                repo.Delete();
            }
        }

        private static ComponentStatusValue CreateComponentStatusValue(string value, DateTimeOffset timestamp, string statusUid = "s1", string componentUid = "c1")
        {
            return new ComponentStatusValue
            {
                ComponentUid = componentUid,
                StatusUid = statusUid,
                Timestamp = timestamp,
                Value = value
            };
        }

        private static void AssertDateTimesAreEqual(DateTimeOffset expected, DateTimeOffset actual)
        {
            var diff = (expected - actual).TotalMilliseconds;
            if (diff > 0.001)
            {
                throw new AssertFailedException("Datetimes do not match.");
            }
        }

        private static HistoryRepository CreateRepository()
        {
            var optionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());

            var componentHistoryRepository = new HistoryRepository();
            componentHistoryRepository.Initialize();

            return componentHistoryRepository;
        }
    }
}
