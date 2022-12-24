using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.History.Repository;

namespace Wirehome.Tests.History
{
    [TestClass]
    public class HistoryValuesStreamTests
    {
        [TestMethod]
        public async Task Append_Single_Value()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await WriteElementAsync(stream, new TimeSpan(00, 00, 00), "19.5", new TimeSpan(12, 30, 59));

            stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            await WriteElementAsync(stream, new TimeSpan(12, 31, 00), "21.5", new TimeSpan(23, 59, 59));

            memory.Seek(0, SeekOrigin.Begin);
            var output = Encoding.UTF8.GetString(memory.ToArray());

            Assert.AreEqual("b000000000 v19.5 e123059000 b123100000 v21.5 e235959000 ", output);
        }

        [TestMethod]
        public async Task Detect_End_Of_Stream_In_Empty_Stream()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            Assert.IsTrue(stream.EndOfStream);
            Assert.IsFalse(await stream.MoveNextAsync());
            Assert.IsFalse(await stream.MovePreviousAsync());
            Assert.IsTrue(stream.EndOfStream);
        }

        [TestMethod]
        public async Task Patch_And_Compare_Previous()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await stream.WriteTokenAsync(new BeginToken(new TimeSpan(00, 00, 00)), CancellationToken.None);
            await stream.WriteTokenAsync(new ValueToken("19.5"), CancellationToken.None);
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(12, 30, 59)), CancellationToken.None);

            memory.Seek(0, SeekOrigin.Begin);

            stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            Assert.IsTrue(await stream.MovePreviousAsync());

            var endToken = stream.CurrentToken as EndToken;
            Assert.AreEqual(new TimeSpan(12, 30, 59), endToken.Value);

            await stream.MovePreviousAsync().ConfigureAwait(false);
            var valueToken = stream.CurrentToken as ValueToken;
            Assert.AreEqual("19.5", valueToken.Value);

            await stream.MoveNextAsync().ConfigureAwait(false);

            // The value is still the same so we patch the end date only.
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(13, 00, 00)), CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Patch_Empty_Stream()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            // End token...
            Assert.IsFalse(await stream.MovePreviousAsync());

            if (stream.CurrentToken == null)
            {
                await stream.WriteTokenAsync(new BeginToken(new TimeSpan(00, 00, 00)), CancellationToken.None);
                await stream.WriteTokenAsync(new ValueToken("19.5"), CancellationToken.None);
                await stream.WriteTokenAsync(new EndToken(new TimeSpan(12, 30, 59)), CancellationToken.None);
            }

            memory.Seek(0, SeekOrigin.Begin);
            var output = Encoding.UTF8.GetString(memory.ToArray());

            Assert.AreEqual("b000000000 v19.5 e123059000 ", output);
        }

        [TestMethod]
        public async Task Patch_Previous_End_Token()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await stream.WriteTokenAsync(new BeginToken(new TimeSpan(00, 00, 00)));
            await stream.WriteTokenAsync(new ValueToken("19.5"));
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(12, 30, 59)));

            memory.Seek(0, SeekOrigin.Begin);

            stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            // End token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            var endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 30, 59), endToken.Value);

            // Patch data...
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(12, 35, 59)));

            Assert.AreEqual("b000000000 v19.5 e123559000 ", Encoding.UTF8.GetString(memory.ToArray()));

            stream = new HistoryValuesStream(memory);
            stream.SeekBegin();

            // Begin token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(BeginToken));
            var beginToken = (BeginToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(00, 00, 00), beginToken.Value);

            // Value token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(ValueToken));
            var valueToken = (ValueToken)stream.CurrentToken;
            Assert.AreEqual("19.5", valueToken.Value);

            // End token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 35, 59), endToken.Value);
        }

        [TestMethod]
        public async Task Read_Previous_Tokens()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await WriteElementAsync(stream, new TimeSpan(00, 00, 00), "19.5", new TimeSpan(12, 30, 59));

            memory.Seek(0, SeekOrigin.Begin);

            stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            // End token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            var endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 30, 59), endToken.Value);

            // Value token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(ValueToken));
            var valueToken = (ValueToken)stream.CurrentToken;
            Assert.AreEqual("19.5", valueToken.Value);

            // Begin token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(BeginToken));
            var beginToken = (BeginToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(00, 00, 00), beginToken.Value);
        }

        [TestMethod]
        public async Task Read_Previous_Tokens_In_Multiple_Data()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await stream.WriteTokenAsync(new BeginToken(new TimeSpan(00, 00, 00)), CancellationToken.None);
            await stream.WriteTokenAsync(new ValueToken("19.5"), CancellationToken.None);
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(12, 30, 59)), CancellationToken.None);
            await stream.WriteTokenAsync(new BeginToken(new TimeSpan(12, 31, 00)), CancellationToken.None);
            await stream.WriteTokenAsync(new ValueToken("20.5"), CancellationToken.None);
            await stream.WriteTokenAsync(new EndToken(new TimeSpan(23, 59, 59)), CancellationToken.None);

            memory.Seek(0, SeekOrigin.Begin);

            stream = new HistoryValuesStream(memory);
            stream.SeekEnd();

            // Iteration 1...

            // End token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            var endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(23, 59, 59), endToken.Value);

            // Value token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(ValueToken));
            var valueToken = (ValueToken)stream.CurrentToken;
            Assert.AreEqual("20.5", valueToken.Value);

            // Begin token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(BeginToken));
            var beginToken = (BeginToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 31, 00), beginToken.Value);

            // Iteration 2...

            // End token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 30, 59), endToken.Value);

            // Value token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(ValueToken));
            valueToken = (ValueToken)stream.CurrentToken;
            Assert.AreEqual("19.5", valueToken.Value);

            // Begin token...
            Assert.IsTrue(await stream.MovePreviousAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(BeginToken));
            beginToken = (BeginToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(00, 00, 00), beginToken.Value);
        }

        [TestMethod]
        public async Task Read_Tokens()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await WriteElementAsync(stream, new TimeSpan(00, 00, 00), "19.5", new TimeSpan(12, 30, 59));

            memory.Seek(0, SeekOrigin.Begin);

            stream = new HistoryValuesStream(memory);

            // Begin token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(BeginToken));
            var beginToken = (BeginToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(00, 00, 00), beginToken.Value);

            // Value token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(ValueToken));
            var valueToken = (ValueToken)stream.CurrentToken;
            Assert.AreEqual("19.5", valueToken.Value);

            // End token...
            Assert.IsTrue(await stream.MoveNextAsync());
            Assert.IsInstanceOfType(stream.CurrentToken, typeof(EndToken));
            var endToken = (EndToken)stream.CurrentToken;
            Assert.AreEqual(new TimeSpan(12, 30, 59), endToken.Value);
        }

        [TestMethod]
        public async Task Write_Two_Single_Values()
        {
            var memory = new MemoryStream();
            var stream = new HistoryValuesStream(memory);

            await WriteElementAsync(stream, new TimeSpan(10, 30, 00), "20", new TimeSpan(11, 45, 00));
            await WriteElementAsync(stream, new TimeSpan(11, 45, 01), "21.5", new TimeSpan(12, 59, 59));

            memory.Seek(0, SeekOrigin.Begin);
            var output = Encoding.UTF8.GetString(memory.ToArray());

            Assert.AreEqual("b103000000 v20 e114500000 b114501000 v21.5 e125959000 ", output);
        }

        async Task WriteElementAsync(HistoryValuesStream stream, TimeSpan begin, string value, TimeSpan end)
        {
            await stream.WriteTokenAsync(new BeginToken(begin)).ConfigureAwait(false);
            await stream.WriteTokenAsync(new ValueToken(value)).ConfigureAwait(false);
            await stream.WriteTokenAsync(new EndToken(end)).ConfigureAwait(false);
        }
    }
}