namespace OSKHelpers.Mail.Test
{
    [TestClass]
    public class MailWorkerTests
    {
        #region Proprietà

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
                var TAG1            = "TAG1";
                var TAG2            = "TAG2";
                var TAG1S           = "XXX";
                var TAG2S           = "YYY";
                var COND1           = "COND1";
                var COND2           = "COND2";
                var condTrue        = () => true;
                var condFalse       = () => false;

                var textWCond1      = "AAA<<COND1>>XXX<</COND1>>BBB";
                var textWCond2      = "CCC<<COND2>>YYY<</COND2>>DDD";
                // Testo con tag aperutra / chiusura inverititi
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
                    // Condizioni
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
                    // Aggiunta di condizioni con nome vuoto o condizione nulla
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condTagEmpty },    textSTag1 + textWCond2 },
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condTagNull },     textSTag1 + textWCond2 },
                    new object[] { textWCond1 + textWCond2,  null, new List<(string, Func<bool>)>() { cond1True, condCond1Null },   textSTag1 + textWCond2 },
                    // Condizioni con errori
                    new object[] { textWCond1Err1,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err1 },
                    new object[] { textWCond1Err2,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err2 },
                    new object[] { textWCond1Err3,  null, new List<(string, Func<bool>)>() { cond1True }, textWCond1Err3 },
                    // Condizioni e Tags
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
                var TAG1        = "TAG1";
                var TAG2        = "TAG2";
                var TAG1S       = "XXX";
                var TAG2S       = "YYY";
                var CONDTRUE    = "COND1";
                var CONDFALSE   = "COND2";
                var condTrueFn  = () => true;
                var condFalseFn = () => false;
                var tags1       = (TAG1, TAG1S);
                var tags2       = (TAG2, TAG2S);

                var condTrue    = (CONDTRUE, condTrueFn);
                var condFalse   = (CONDFALSE, condFalseFn);

                var fileContent = @"
# Commento
@Subject: Test {TAG1} <<COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>>
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContent2Subject1 = @"
# Commento
<<COND1>>@Subject: Test {TAG1} <</COND1>>
<<COND2>>@Subject: Test {TAG2} <</COND2>>
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContent2Subject2 = @"
# Commento
<<COND2>>@Subject: Test {TAG1} <</COND2>>
<<COND1>>@Subject: Test {TAG2} <</COND1>>
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContent2Subject1Error = @"
# Commento
<<COND1>>@Subject: Test {TAG1} <</COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND1>>
<<COND2>>@Subject: Test {TAG1} <</COND2>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND2>>
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContent2Subject2Error = @"
# Commento
<<COND2>>@Subject: Test {TAG1} <</COND2>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>><</COND2>>
<<COND1>>@Subject: Test {TAG1} <</COND1>>{TAG2}<</COND1>> <<COND2>>{TAG1}<</COND2>><</COND1>>
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContentNoSubject = @"
# Commento
# Commento 2
Testo <<COND1>>Interno Condizione Vera {TAG1}<</COND1>> <<COND1>>Interno   
Condizione Vera Multiline {TAG1}<</COND1>> <<COND2>>Interno Condizione Falsa<</COND2>> {TAG2} <<COND2>>Interno 
Condizione Falsa Miltiline<</COND2>> FINE
";
                var fileContentNoBody = @"
# Commento
@Subject: Test {TAG1} <<COND1>>{TAG1}<</COND1>> <<COND2>>{TAG2}<</COND2>>
# Commento 2

";
                var mailSubject = "Test XXX XXX";
                var mail2Subject1 = "Test XXX";
                var mail2Subject2 = "Test YYY";
                var mailBody    = "Testo Interno Condizione Vera XXX Interno Condizione Vera Multiline XXX  YYY  FINE";

                var objects = new List<object[]>()
                {
                    // Lista righe vuota o nulla
                    new object[] { null, null, null, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    new object[] { new List<string>(), null, null, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // Contenuto valido
                    new object[] { fileContent.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mailSubject, mailBody, (Dictionary<string, string>)null) },
                    // Oggetto assente
                    new object[] { fileContentNoSubject.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // Nessuna riga per il corpo
                    new object[] { fileContentNoBody.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    // Due oggetti, alternativamente validi il primo o il secondo. I primi due corretti, i secondi contengono un errore nei tag (test di timeout)
                    new object[] { fileContent2Subject1.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mail2Subject1, mailBody, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject2.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (true, mail2Subject2, mailBody, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject1Error.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) },
                    new object[] { fileContent2Subject2Error.Split("\r\n").ToList(), new List<(string, string)> { tags1, tags2 }, new List<(string, Func<bool>)> { condTrue, condFalse }, (false, string.Empty, string.Empty, (Dictionary<string, string>)null) }
                };

                return objects;
            }
        }

        #endregion

        #region Metodi

        [TestMethod]
        [DynamicData(nameof(ParseStringTestsData))]
        public void ParseStringTests(string text, List<(string, string)> tags, List<(string, Func<bool>)> conditions, string expected)
        {
            Assert.AreEqual(expected, MailWorker.ParseString(text, tags, conditions));
        }

        [TestMethod]
        [DynamicData(nameof(ParseFileTestsData))]
        public void ParseFileTests(List<string> lines, List<(string, string)> tags, List<(string, Func<bool>)> conditions, (bool Parsed, string Subject, string Body, Dictionary<string, string>) expected)
        {
            Assert.AreEqual(expected, MailWorker.ParseFile(lines, tags, conditions));
        }


        #endregion

    }
}