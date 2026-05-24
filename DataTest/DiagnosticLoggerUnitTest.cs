using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using TP.ConcurrentProgramming.Data;

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class DiagnosticLoggerUnitTest
  {
    [TestMethod]
    [Timeout(3000)]
    public void DiagnosticLoggerShouldDropEntriesWhenBufferIsFull()
    {
      string logFilePath = Path.Combine(Path.GetTempPath(), $"diagnostic_test_{Guid.NewGuid()}.txt");

      using DiagnosticLogger logger = new DiagnosticLogger(logFilePath, maximumBufferSize: 1);

      for (int i = 0; i < 1000; i++)
      {
        logger.Log($"test-entry-{i}");
      }

      Assert.IsTrue(logger.DroppedEntries > 0, "Logger should drop entries when the diagnostic buffer is full.");
    }
  }
}