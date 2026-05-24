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
using System.IO;

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

      string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
      Directory.CreateDirectory(logsDirectory);
      string logFilePath = Path.Combine(logsDirectory, "diagnostic_log.txt");
      Debug.WriteLine($"Diagnostic log path: {logFilePath}");
      diagnosticLogger = new DiagnosticLogger(logFilePath);

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

        bool isControlledByUser = i == 0;

        Vector startingVelocity = isControlledByUser ? new Vector(0.0, 0.0) : new Vector(velocityX, velocityY);

        Ball newBall = new Ball(
          startingPosition,
          startingVelocity,
          diameter,
          BallsList,
          physicLock,
          isControlledByUser);

        if (isControlledByUser)
        {
          controlledBall = newBall;
        }

        int ballId = i;

        newBall.NewPositionNotification += (_, position) =>
        {
          // czas_zdarzenia; numer_kulki; pozycja_X; pozycja_Y; prędkość_X; prędkość_Y
          diagnosticLogger?.Log($"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()};{ballId};{position.x:0.000};{position.y:0.000};{newBall.Velocity.x:0.000};{newBall.Velocity.y:0.000}");
        };

        lock (ballsListLock)
        {
          BallsList.Add(newBall);
        }
        upperLayerHandler(startingPosition, newBall);
      }

      foreach (Ball ball in BallsList)
        ball.Start(Width, Height);
    }
    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          foreach (var ball in BallsList)
          {
            ball.Stop();
          }

          lock (ballsListLock)
          {
            BallsList.Clear();
          }

          diagnosticLogger?.Dispose();
          diagnosticLogger = null;
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

    #endregion IDisposable

    #region private

    private bool Disposed = false;

    private List<Ball> BallsList = [];
    private readonly object ballsListLock = new object();

    private DiagnosticLogger? diagnosticLogger;

    private Ball? controlledBall;

    private readonly Stopwatch controlledBallStopwatch = Stopwatch.StartNew();
    private long lastControlledBallUpdateMs = 0;

    public override double Width { get; } = 420;
    public override double Height { get; } = 400;

    public override IVector TotalMomentum
    {
      get
      {
        double totalX = 0.0;
        double totalY = 0.0;

        List<Ball> ballsSnapshot;

        lock (ballsListLock)
        {
          ballsSnapshot = BallsList.ToList();
        }

        foreach (Ball ball in ballsSnapshot)
        {
          if (ball.IsControlledByUser)
            continue;

          totalX += ball.Velocity.x;
          totalY += ball.Velocity.y;
        }

        return new Vector(totalX, totalY);
      }
    }

    public override double TotalKineticEnergy
    {
      get
      {
        double totalEnergy = 0.0;

        List<Ball> ballsSnapshot;

        lock (ballsListLock)
        {
          ballsSnapshot = BallsList.ToList();
        }

        foreach (Ball ball in ballsSnapshot)
        {
          if (ball.IsControlledByUser)
            continue;

          double velocityX = ball.Velocity.x;
          double velocityY = ball.Velocity.y;

          double speedSquared = velocityX * velocityX + velocityY * velocityY;

          // Gdy różne masy, tutaj trzeba będzie użyć ball.Mass
          totalEnergy += 0.5 * speedSquared;
        }

        return totalEnergy;
      }
    }

    public override void SetControlledBallPosition(double x, double y)
    {
      Ball? ball = controlledBall;

      if (ball == null)
        return;

      double radius = ball.Diameter / 2.0;

      double maxX = Width - ball.Diameter - 4 * 2;
      double maxY = Height - ball.Diameter - 4 * 2;

      double clampedX = Math.Clamp(x - radius, 0.0, maxX);
      double clampedY = Math.Clamp(y - radius, 0.0, maxY);

      long currentTimeMs = controlledBallStopwatch.ElapsedMilliseconds;
      double elapsedTimeMs = currentTimeMs - lastControlledBallUpdateMs;

      if (lastControlledBallUpdateMs == 0)
      {
        elapsedTimeMs = 16.0;
      }

      lastControlledBallUpdateMs = currentTimeMs;

      ball.SetControlledPosition(new Vector(clampedX, clampedY), elapsedTimeMs);

      ball.ResolveCollisions();

      ball.Velocity = new Vector(0.0, 0.0);
    }

    private readonly object physicLock = new();

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
    {
      returnBallsList(BallsList);
    }

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
    {
      returnNumberOfBalls(BallsList.Count);
    }

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}