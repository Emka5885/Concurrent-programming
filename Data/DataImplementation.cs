//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class DataImplementation : DataAbstractAPI
  {
    #region DataAbstractAPI

    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
     
      Random random = new Random();
      
      for (int i = 0; i < numberOfBalls; i++)
      {
        double diameter = 20.0;

        double maxX = Width - diameter - 2 * 4.0;
        double maxY = Height - diameter - 2 * 4.0;

        double x = random.NextDouble() * maxX;
        double y = random.NextDouble() * maxY;
        Vector startingPosition = new(x, y);

        double velocityX = (random.NextDouble() - 0.5) * 10;
        double velocityY = (random.NextDouble() - 0.5) * 10;

        if (Math.Abs(velocityX) < 0.1) velocityX = 2.0;
        if (Math.Abs(velocityY) < 0.1) velocityY = 2.0;

        Vector startingVelocity = new(velocityX, velocityY);

        Ball newBall = new(startingPosition, startingVelocity, diameter);

        lock (ballsListLock)
        {
          BallsList.Add(newBall);
        }

        upperLayerHandler(startingPosition, newBall);
      }

      StartSimulationLoop();
    }
    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          simulationCancellation?.Cancel();

          try
          {
            simulationTask?.Wait(500);
          }
          catch (AggregateException)
          {
            // Task może zostać przerwany przez anulowanie tokenu.
            // Normalne przy zamykaniu
          }

          lock (ballsListLock)
          {
            BallsList.Clear();
          }

          simulationCancellation?.Dispose();
          simulationCancellation = null;
          simulationTask = null;
        }

        Disposed = true;
      }
      else
        throw new ObjectDisposedException(nameof(DataImplementation));
    }

    public override void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    private void StartSimulationLoop()
    {
      if (simulationTask != null && !simulationTask.IsCompleted)
        throw new InvalidOperationException("Simulation is already running.");

      simulationCancellation = new CancellationTokenSource();
      CancellationToken token = simulationCancellation.Token;

      simulationTask = Task.Run(async () =>
      {
        const double targetFrameTimeMs = 30.0;

        Stopwatch stopwatch = Stopwatch.StartNew();
        double nextFrameTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        while (!token.IsCancellationRequested)
        {
          List<Ball> ballsSnapshot;

          lock (ballsListLock)
          {
            ballsSnapshot = BallsList.ToList();
          }

          foreach (Ball ball in ballsSnapshot)
          {
            ball.MoveBall(Width, Height);
          }

          nextFrameTimeMs += targetFrameTimeMs;

          double delayMs = nextFrameTimeMs - stopwatch.Elapsed.TotalMilliseconds;

          if (delayMs > 0)
          {
            await Task.Delay((int)delayMs, token);
          }
          else
          {
            nextFrameTimeMs = stopwatch.Elapsed.TotalMilliseconds;
          }
        }
      }, token);
    }

    #endregion IDisposable

    #region private

    //private bool disposedValue;
    private bool Disposed = false;

    private readonly List<Ball> BallsList = [];
    private readonly object ballsListLock = new object();

    private CancellationTokenSource? simulationCancellation;
    private Task? simulationTask;

    public override double Width { get; } = 420;
    public override double Height { get; } = 400;


    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
    {
      lock (ballsListLock)
      {
        returnBallsList(BallsList.ToList());
      }
    }

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
    {
      lock (ballsListLock)
      {
        returnNumberOfBalls(BallsList.Count);
      }
    }

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}