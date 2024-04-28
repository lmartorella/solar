using FluentAssertions;
using FluentAssertions.Execution;
using Lucky.Db;
using Lucky.Home.Db;
using Lucky.Home.Solar;
using System.Diagnostics;

namespace Home.Solar.Tests
{
    [TestClass]
    public class AggregationTest
    {
        private int CountLines(FileInfo fileInfo)
        {
            using (var reader = fileInfo.OpenText())
            {
                int count = 0;
                while (!reader.EndOfStream)
                {
                    var str = reader.ReadLine();
                    if (str != null && str.Length > 0)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        [TestMethod]
        public void TestNoExceptionReadingOldCsvs()
        {
            // Expect no exceptions
            var files = CsvAggregate<PowerData, DayPowerData>.GetFilesInFolder(new DirectoryInfo(@"..\..\..\etc\db"), FsTimeSeries<PowerData, DayPowerData>.FILENAME_FORMAT, DateTime.Now);
            using (new AssertionScope())
            {
                foreach (var file in files)
                {
                    // Skip empty files
                    var lineCount = CountLines(file.Item1);
                    if (file.Item1.Name == "2017-07-08.csv")
                    {
                        Debugger.Break();
                    }
                    var data = CsvAggregate<PowerData, DayPowerData>.ParseCsv(file.Item1, file.Item2);
                    if (lineCount > 1)
                    {
                        data.Should().NotBeNull("Error in file " + file.Item1.Name);
                    }
                }
            }
        }

        [TestMethod]
        public void TestOldCsvFormat()
        {
            var data = CsvAggregate<PowerData, DayPowerData>.ParseCsv(new FileInfo(@"Resources\oldFormatNoFault.csv"), DateTime.Now);
            Assert.AreEqual(0, data.Fault);

            var data2 = CsvAggregate<PowerData, DayPowerData>.ParseCsv(new FileInfo(@"Resources\oldFormatFault.csv"), DateTime.Now);
            Assert.AreEqual(1, data2.Fault);
        }
    }
}