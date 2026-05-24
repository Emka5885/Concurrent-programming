using System.Collections.Concurrent;
using System.Text;

namespace TP.ConcurrentProgramming.Data
{
  internal sealed class DiagnosticLogger : IDisposable
  {
    internal DiagnosticLogger(string filePath, int maximumBufferSize = 10_000)
    {
      this.filePath = filePath;
      this.maximumBufferSize = maximumBufferSize;

      writerCancellation = new CancellationTokenSource();
      writerTask = Task.Run(() => WriteLoop(writerCancellation.Token));
    }

    internal int DroppedEntries => droppedEntries;

    internal void Log(string message)
    {

      if (queue.Count >= maximumBufferSize)
      {
        Interlocked.Increment(ref droppedEntries);
        return;
      }

      queue.Enqueue(message);
      signal.Release();
    }

    private async Task WriteLoop(CancellationToken token)
    {
      using StreamWriter writer = new StreamWriter(filePath, append: false, Encoding.ASCII);

      while (!token.IsCancellationRequested)
      {
        try
        {
          await signal.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
          break;
        }

        while (queue.TryDequeue(out string? message))
        {
          await writer.WriteLineAsync(message);
        }

        await writer.FlushAsync();
      }

      while (queue.TryDequeue(out string? message))
      {
        await writer.WriteLineAsync(message);
      }

      await writer.FlushAsync();
    }

    public void Dispose()
    {
      writerCancellation.Cancel();

      try
      {
        writerTask.Wait(1000);
      }
      catch (AggregateException)
      {
      }

      writerCancellation.Dispose();
      signal.Dispose();
    }

    private readonly string filePath;
    private readonly int maximumBufferSize;

    private readonly ConcurrentQueue<string> queue = new();
    private readonly SemaphoreSlim signal = new(0);

    private readonly CancellationTokenSource writerCancellation;
    private readonly Task writerTask;

    private int droppedEntries = 0;
  }
}