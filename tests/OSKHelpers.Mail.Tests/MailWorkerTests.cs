using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSKHelpers.Mail.Tests
{
    [TestClass]
    public class MailWorkerTests
    {
        #region Properties

        private static IEnumerable<object[]> ParseStringTestsData
        {
            get
            {
                var textEmpty       = string.Empty;
                var textWTag1       = "AAA{TAG1}BBB";
                var textWTag2       = "CCC{TAG2}DDD";
                var textSTag1       = "AAAXXXBBB";
                var textSTag2       = "CCCYYYDDD";
                var textETag1       = "AAABBB";
                var textETag2       = "CCCDDD";
                const string TAG1   = "TAG1";
                const string TAG2   = "TAG2";
                const string TAG1S  = "XXX";
                const string TAG2S  = "YYY";
                const string COND1  = "COND1";
                const string COND2  = "COND2";
                var condTrue        = () => true;
                var condFalse       = () => false;

                var textWCond1      = "AAA<<COND1>>XXX<</COND1>>BBB";
                var textWCond2      = "CCC<<COND2>>YYY<</COND2>>DDD";
                // Text with opening/closing tags reversed
                var textWCond1Err1  = "AAA<</COND1>>XXX<<COND1>>BBB";
                var textWCond1Err2  = "AAA<<COND1>>XXXBBB";
                var textWCond1Err3  = "AAA<XXX<</COND1>>BBB";

                var tags1 = (TAG1, TAG1S);
                var tags2 = (TAG2, TAG2S);

                var cond1True   = (COND1, condTrue);
                var cond1False  = (COND1, condFalse);
                var cond2True   = (COND2, condTrue);
                var cond2False  = (COND2, condFalse);
                var condTagEmpty = (string.Empty, condTrue);
                var condTagNull  = ((string)null, condTrue);
                var condCond1Null = (COND1, (Func<bool>)null);

                var objects = new List<object[]>()
                {
                    new object[] { null,        new List<(string, string)>() { tags1 }, new List<(string, Func<bool>)>() { cond1True }, textEmpty },
                    new object[] { textEmpty,   new List<(string, string)>() { tags1 }, new List<(string, Func<bool>)>() { cond1True }, textEmpty },
                    // Tags
                    new object[] { textWTag1,   new List<(string, string)>() { tags1 }, null, textSTag1 },
                    new object[] { textWTag2,   new List<(string, string)>() { tags2 }, null, textSTag2 },
                    new object[] { textWTag1,   new List<(string, string)>() { tags2 }, null, textWTag1 },
                    new object[] { textWTag2,   new List<(string, string)>() { tags1 }, null, textWTag2 },
                    new object[] { textWTag1 + textWTag2,   new List<(string, string)>() { tags1, tags2 }, null, textSTag1 + textSTag2 },
                    // Conditions
                    new object[] { textWCond1,  null, new List<(string, Func<bool>)>() { cond1True },   textSTag1 },
                    new object[] { textWCond1,  null, new List<(string, Func<bool>)>() { cond1False },  textETag1 },
                    new object[] { textWCond2,  null, new List<(string, Func<bool>)>() { cond2True },   textSTag2 },
                    new object[] { textWCond2,  null, new List<(string, Func<bool>)>() { cond2False },  textETag2 },
                    new object[] { textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1True, cond2True },    textSTag1 + textSTag2 },
                    new object[] { textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1False, cond2False },  textETag1 + textETag2 },
                    new object[] { textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1True, cond2False },   textSTag1 + textETag2 },
                    new object[] { textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1False, cond2True },   textETag1 + textSTag2 },
                    new object[] { textWCond1 + textWCond1, null, new List<(string, Func<bool>)>() { cond1True },               textSTag1 + textSTag1 },
                    new object[] { textWCond1 + textWCond1, null, new List<(string, Func<bool>)>() { cond1False },              textETag1 + textETag1 },
                    new object[] { textWCond1 + textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1True, cond2False }, textSTag1 + textSTag1 + textETag2 },
                    new object[] { textWCond1 + textWCond1 + textWCond2, null, new List<(string, Func<bool>)>() { cond1False, cond2True }, textETag1 + textETag1 + textSTag2 },
                    // Adding conditions with empty name or null condition
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condTagEmpty },    textSTag1 + textWCond2 },
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condTagNull },     textSTag1 + textWCond2 },
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condCond1Null },   textSTag1 + textWCond2 },
                    // Conditions with errors
                    new object[] { textWCond1Err1,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err1 },
                    new object[] { textWCond1Err2,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err2 },
                    new object[] { textWCond1Err3,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err3 },
                    // Conditions and Tags
                    new object[] { textWTag1 + textWTag2 + textWCond1 + textWCond2, new List<(string, string)>() { tags1, tags2 },  new List<(string, Func<bool>)>() { cond1True, cond2True },  textSTag1 + textSTag2 + textSTag1 + textSTag2 },
                    new object[] { textWTag1 + textWTag2 + textWCond1 + textWCond2, new List<(string, string)>() { tags1 },         new List<(string, Func<bool>)>() { cond1True, cond2False }, textSTag1 + textWTag2 + textSTag1 + textETag2 }

                };

                return objects;
            }
        }

        private static IEnumerable<object[]> ParseFileTestsData
        {
            get
            {
                const string TAG1      = "TAG1";
                const string TAG2      = "TAG2";
                const string TAG1S     = "XXX";
                const string TAG2S     = "YYY";
                const string CONDTRUE  = "COND1";
                const string CONDFALSE = "COND2";
                var condTrueFn         = () => true;
                var condFalseFn        = () => false;
                var tags1       = (TAG1, TAG1S);
                var tags2       = (TAG2, TAG2S);

                var condTrue    = (CONDTRUE, condTrueFn);
                var condFalse   = (CONDFALSE, condFalseFn);

                var fileContent = @"
# Comment
@Subject: Test {TAG1} <<COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>>
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContent2Subject1 = @"
# Comment
<<COND1>>@Subject: Test {TAG1} <</COND1>>
<<COND2>>@Subject: Test {TAG2} <</COND2>>
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContent2Subject2 = @"
# Comment
<<COND2>>@Subject: Test {TAG1} <</COND2>>
<<COND1>>@Subject: Test {TAG2} <</COND1>>
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContent2Subject1Error = @"
# Comment
<<COND1>>@Subject: Test {TAG1} <</COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND1>>
<<COND2>>@Subject: Test {TAG1} <</COND2>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND2>>
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContent2Subject2Error = @"
# Comment
<<COND2>>@Subject: Test {TAG1} <</COND2>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND2>>
<<COND1>>@Subject: Test {TAG1} <</COND1>>{TAG2}<</COND1>> <<COND2>>{TAG1}<</COND2>><</COND1>>
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContentNoSubject = @"
# Comment
# Comment 2
Text <<COND1>>Inside True Condition {TAG1}<</COND1>> <<COND1>>Inside True  
Condition Multiline {TAG1}<</COND1>> <<COND2>>Inside False Condition<</COND2>> {TAG2} <<COND2>>Inside 
False Condition Multiline<</COND2>> END
";
                var fileContentNoBody = @"
# Comment
@Subject: Test {TAG1} <<COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>>
# Comment 2

";
                var mailSubject = "Test XXX XXX";
                var mail2Subject1 = "Test XXX";
                var mail2Subject2 = "Test YYY";
                var mailBody    = "Text Inside True Condition XXX Inside True Condition Multiline XXX  YYY  END";

                var objects = new List<object[]>()
                {
                    // Empty or null row list
                    new object[] { null, null, null, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    new object[] { new List<string>(), null, null, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // Valid content
                    new object[] { fileContent.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mailSubject, mailBody, (Dictionary<string, string>)null) },
                    // No subject
                    new object[] { fileContentNoSubject.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // No body lines
                    new object[] { fileContentNoBody.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // Two subjects, alternately valid: first or second. First two correct, second two contain a tag error (timeout test)
                    new object[] { fileContent2Subject1.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mail2Subject1, mailBody, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject2.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mail2Subject2, mailBody, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject1Error.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject2Error.Replace("\r\n", "\n").Split('\n').ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) }
                };

                return objects;
            }
        }

        #endregion

        #region Methods

        [TestMethod]
        [DynamicData(nameof(ParseStringTestsData))]
        public void ParseString_VariousContents_ReturnsExpected(string text, List<(string, string)> tags, List<(string, Func<bool>)> conditions, string expected)
        {
            Assert.AreEqual(expected, MailWorker.ParseString(text, tags, conditions));
        }

        [TestMethod]
        [DynamicData(nameof(ParseFileTestsData))]
        public void ParseFile_VariousTemplates_ReturnsExpected(List<string> lines, List<(string, string)> tags, List<(string, Func<bool>)> conditions, (bool Parsed, string Subject, string Body, Dictionary<string, string>) expected)
        {
            Assert.AreEqual(expected, MailWorker.ParseFile(lines, tags, conditions));
        }

        [TestMethod]
        [DataRow(null,                  "to@foo.it",    "Subject",  null)]
        [DataRow("from@foo.it",         null,           "Subject",  "Body")]
        [DataRow("from@foo.it",         "to@foo.it",    null,       "Body")]
        [DataRow(null,                  null,           null,       null)]
        public async Task SendAsync_NullData_ReturnsFalse(string from, string to, string subject, string body)
        {
            // Validation fails before connecting, so the server is never contacted
            var recipients = to == null ? null : new List<string>() { to };

            var sent = await MailWorker.SendAsync("localhost", 25, false, null, null, from, recipients, subject, body);

            Assert.IsFalse(sent);
        }

        /// <summary>
        /// Verifies that an empty list of recipients or an empty body make the send fail before connecting.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_EmptyRecipientsOrBody_ReturnsFalse()
        {
            Assert.IsFalse(await MailWorker.SendAsync("localhost", 25, false, null, null, "from@example.com", new List<string>(), "Subject", "Body"));
            Assert.IsFalse(await MailWorker.SendAsync("localhost", 25, false, null, null, "from@example.com", new List<string> { "to@example.com" }, "Subject", " "));
        }

        /// <summary>
        /// Verifies that a connection error (local server on a closed port) returns false without exceptions.
        /// </summary>
        [TestMethod]
        public async Task SendAsync_ServerNotReachable_ReturnsFalse()
        {
            var sent = await MailWorker.SendAsync("127.0.0.1", 1, false, null, null, "from@example.com", new List<string> { "to@example.com" }, "Subject", "Body");

            Assert.IsFalse(sent);
        }

        /// <summary>
        /// Verifies that the overloads without sender return false when SmtpFrom is not a valid address.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not an address")]
        public async Task SendAsyncAndSendReplyToAsync_InvalidSmtpFrom_ReturnFalse(string smtpFrom)
        {
            MailWorker.SetupSmtp("127.0.0.1", 1, false, null, null, smtpFrom);
            try
            {
                Assert.IsFalse(await MailWorker.SendAsync("to@example.com", "Subject", "Body"));
                Assert.IsFalse(await MailWorker.SendAsync(new List<string> { "to@example.com" }, "Subject", "Body"));
                Assert.IsFalse(await MailWorker.SendReplyToAsync("to@example.com", "reply@example.com", "Subject", "Body"));
                Assert.IsFalse(await MailWorker.SendReplyToAsync(new List<string> { "to@example.com" }, new List<string> { "reply@example.com" }, "Subject", "Body"));
                Assert.IsFalse(MailWorker.Send("to@example.com", "Subject", "Body"));
                Assert.IsFalse(MailWorker.SendReplyTo("to@example.com", "reply@example.com", "Subject", "Body"));
            }
            finally
            {
                MailWorker.SetupSmtp(null, 0, false, null, null, null);
            }
        }

        /// <summary>
        /// Verifies that the synchronous Send with explicit server returns the result of SendAsync (false for invalid data).
        /// </summary>
        [TestMethod]
        public void Send_InvalidData_ReturnsFalse()
        {
            Assert.IsFalse(MailWorker.Send("localhost", 25, false, null, null, null, new List<string> { "to@example.com" }, "Subject", "Body"));
        }

        #endregion

        #region ParseString / ParseFile (additional cases)

        /// <summary>
        /// Verifies that null or empty content returns an empty string.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void ParseString_EmptyContent_ReturnsEmptyString(string content)
        {
            Assert.AreEqual(string.Empty, MailWorker.ParseString(content, null, (List<(string, bool)>)null));
        }

        /// <summary>
        /// Verifies the replacement of more tags, also repeated, leaving unknown tags unchanged.
        /// </summary>
        [TestMethod]
        public void ParseString_MultipleTags_ReplacesAllOccurrences()
        {
            var tags = new List<(string Name, string Value)> { ("NAME", "Mario"), ("CODE", "42") };

            var result = MailWorker.ParseString("{NAME} {CODE} {NAME} {OTHER}", tags, (List<(string, bool)>)null);

            Assert.AreEqual("Mario 42 Mario {OTHER}", result);
        }

        /// <summary>
        /// Verifies that tag names are case sensitive.
        /// </summary>
        [TestMethod]
        public void ParseString_TagDifferentCase_IsNotReplaced()
        {
            var tags = new List<(string Name, string Value)> { ("name", "Mario") };

            Assert.AreEqual("{NAME}", MailWorker.ParseString("{NAME}", tags, (List<(string, bool)>)null));
        }

        /// <summary>
        /// Verifies nested conditions: the outer false condition removes also the inner section.
        /// </summary>
        [TestMethod]
        [DataRow(true, true, "A B C D E")]
        [DataRow(true, false, "A B D E")]
        [DataRow(false, true, "A E")]
        public void ParseString_NestedConditions_KeepsOrRemovesSections(bool outer, bool inner, string expected)
        {
            var conditions = new List<(string Name, bool Check)> { ("OUTER", outer), ("INNER", inner) };

            var result = MailWorker.ParseString("A <<OUTER>>B <<INNER>>C <</INNER>>D <</OUTER>>E", null, conditions);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that conditions with a null function or an empty name are ignored.
        /// </summary>
        [TestMethod]
        public void ParseString_NullFunctionConditions_AreIgnored()
        {
            var conditions = new List<(string Name, Func<bool> Check)> { ("COND", null), ("", () => false) };

            Assert.AreEqual("A <<COND>>B<</COND>>", MailWorker.ParseString("A <<COND>>B<</COND>>", null, conditions));
        }

        /// <summary>
        /// Verifies that ParseFile with null or empty lines returns a result not parsed.
        /// </summary>
        [TestMethod]
        public void ParseFile_NullOrEmptyLines_ReturnsNotParsed()
        {
            var nullResult  = MailWorker.ParseFile(null, null, (List<(string, bool)>)null);
            var emptyResult = MailWorker.ParseFile(new List<string>(), null, (List<(string, bool)>)null);

            Assert.IsFalse(nullResult.Parsed);
            Assert.AreEqual(string.Empty, nullResult.Subject);
            Assert.AreEqual(string.Empty, nullResult.Body);
            Assert.IsFalse(emptyResult.Parsed);
        }

        /// <summary>
        /// Verifies that ParseFile removes comments and empty lines, extracts the subject and joins the body lines.
        /// </summary>
        [TestMethod]
        public void ParseFile_CommentsAndEmptyLines_ExtractsSubjectAndBody()
        {
            var lines = new List<string> { "# Comment", "", "@Subject: Hello {NAME}", "   ", "Line 1", "# Other comment", "Line 2" };
            var tags  = new List<(string Name, string Value)> { ("NAME", "Mario") };

            var result = MailWorker.ParseFile(lines, tags, (List<(string, bool)>)null);

            Assert.IsTrue(result.Parsed);
            Assert.AreEqual("Hello Mario", result.Subject);
            Assert.AreEqual("Line 1 Line 2", result.Body);
            Assert.IsNull(result.TagValues);
        }

        /// <summary>
        /// Verifies that empty custom tag names are ignored and a missing custom tag has an empty value.
        /// </summary>
        [TestMethod]
        public void ParseFile_CustomTagsWithEmptyAndMissing_ReturnsExpectedValues()
        {
            var lines       = new List<string> { "@Subject: Subject", "@Alt: Alternative", "Body" };
            var customTags  = new List<string> { "Alt", " ", "Missing" };

            var result = MailWorker.ParseFile(lines, null, (List<(string, bool)>)null, customTags);

            Assert.IsTrue(result.Parsed);
            Assert.HasCount(2, result.TagValues);
            Assert.AreEqual("Alternative", result.TagValues["Alt"]);
            Assert.AreEqual(string.Empty, result.TagValues["Missing"]);
            Assert.AreEqual("Body", result.Body);
        }

        #endregion

        #region Integration (local SMTP server)

        /// <summary>
        /// Integration test: sends a message to a minimal local SMTP server and verifies envelope and content received.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task SendAsync_LocalSmtpServer_DeliversMessage()
        {
            using var server = new FakeSmtpServer();
            var serverTask = server.RunAsync();

            var sent = await MailWorker.SendAsync("127.0.0.1", server.Port, false, null, null, "from@example.com",
                new List<string> { "to@example.com" }, "Integration subject", "<p>Integration body</p>", replyTo: new List<string> { "reply@example.com" });
            await serverTask;

            Assert.IsTrue(sent);
            Assert.Contains("MAIL FROM:<from@example.com>", server.Commands);
            Assert.Contains("RCPT TO:<to@example.com>", server.Commands);
            Assert.Contains("Subject: Integration subject", server.Data);
            Assert.Contains("Reply-To: reply@example.com", server.Data);
            Assert.Contains("Integration body", server.Data);
        }

        /// <summary>
        /// Integration test: with startTls, a server that does not support STARTTLS makes the send fail instead of
        /// sending the message without encryption.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task SendAsync_StartTlsNotSupportedByServer_ReturnsFalse()
        {
            using var server = new FakeSmtpServer();
            var serverTask = server.RunAsync();

            var sent = await MailWorker.SendAsync("127.0.0.1", server.Port, false, null, null, "from@example.com",
                new List<string> { "to@example.com" }, "Subject", "Body", startTls: true);
            await serverTask;

            Assert.IsFalse(sent);
            Assert.DoesNotContain("DATA", server.Commands);
        }

        /// <summary>
        /// Minimal SMTP server on the loopback interface: accepts one connection, answers the commands of a plain
        /// (not encrypted, not authenticated) session and records commands and message data.
        /// </summary>
        private sealed class FakeSmtpServer : IDisposable
        {
            private readonly System.Net.Sockets.TcpListener _listener;

            public int Port { get; }

            public string Commands { get; private set; }

            public string Data { get; private set; }

            public FakeSmtpServer()
            {
                _listener   = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                _listener.Start();
                Port        = ((System.Net.IPEndPoint)_listener.LocalEndpoint).Port;
                Commands    = string.Empty;
                Data        = string.Empty;
            }

            /// <summary>
            /// Serves a single SMTP session, until QUIT or the closing of the connection.
            /// </summary>
            public async Task RunAsync()
            {
                using var client    = await _listener.AcceptTcpClientAsync();
                using var stream    = client.GetStream();
                using var reader    = new System.IO.StreamReader(stream);
                using var writer    = new System.IO.StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };
                var commands        = new System.Text.StringBuilder();
                var data            = new System.Text.StringBuilder();
                var quit            = false;

                await writer.WriteLineAsync("220 localhost ESMTP test server");
                while (!quit)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        quit = true;
                    }
                    else
                    {
                        commands.AppendLine(line);
                        var command = line.Length >= 4 ? line.Substring(0, 4).ToUpperInvariant() : line.ToUpperInvariant();
                        switch (command)
                        {
                            case "EHLO":
                                await writer.WriteLineAsync("250-localhost");
                                await writer.WriteLineAsync("250 8BITMIME");
                                break;
                            case "DATA":
                                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                                string dataLine;
                                while ((dataLine = await reader.ReadLineAsync()) != null && dataLine != ".")
                                {
                                    data.AppendLine(dataLine);
                                }
                                await writer.WriteLineAsync("250 OK");
                                break;
                            case "QUIT":
                                await writer.WriteLineAsync("221 Bye");
                                quit = true;
                                break;
                            case "STAR":
                                await writer.WriteLineAsync("502 Command not implemented");
                                break;
                            default:
                                await writer.WriteLineAsync("250 OK");
                                break;
                        }
                    }
                }

                Commands    = commands.ToString();
                Data        = data.ToString();
            }

            public void Dispose()
            {
                _listener.Stop();
            }
        }

        #endregion

    }
}