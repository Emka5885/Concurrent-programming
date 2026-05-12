using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(Vector initialPosition, Vector initialVelocity, double diameter, List<Ball> allBalls, object physicLock)
    {
      position = initialPosition;
      Velocity = initialVelocity;
      this.diameter = diameter;
      this.allBalls = allBalls;
      this.physicLock = physicLock;
    }

    #endregion

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;
    public IVector Velocity { get; set; }
    public IVector Position => position;
    public double Diameter => diameter;

    #endregion

    #region private

    private Vector position;
    private readonly double diameter;
    private CancellationTokenSource? cts;

    private readonly List<Ball> allBalls;
    private readonly object physicLock;

    private const int TargetIntervalMs = 16;
    private const long MaxCumulativeLatencyMs = 500;

    public void Start(double width, double height)
    {
      cts = new CancellationTokenSource();
      CancellationToken token = cts.Token;

      ValidateVelocity(width, height);

      Task.Run(async () =>
      {
        // Stopwatch wysoka rozdzielczość
        Stopwatch sw = Stopwatch.StartNew();
        int tickNumber = 0;

        while (!token.IsCancellationRequested)
        {
          tickNumber++;

          // Kiedy powinien nastąpić ten tick (w ms od startu)
          long expectedMs = (long)tickNumber * TargetIntervalMs;

          lock (physicLock)
          {
            MoveBall(width, height);
            ResolveCollisions();
          }

          long nowMs = sw.ElapsedMilliseconds;
          long waitMs = expectedMs - nowMs;

          // Łączne spóźnienie względem harmonogramu
          long latency = nowMs - expectedMs;
          if (latency > MaxCumulativeLatencyMs)
          {
            // Narastające opóźnienie przekroczyło próg — resetuj
            sw.Restart();
            tickNumber = 0;
            throw new InvalidOperationException($"Ball physics is lagging behind schedule by {latency} ms.");
          }

          int delayMs = (int)Math.Max(1, waitMs);

          try
          {
            await Task.Delay(delayMs, token);
          }
          catch (OperationCanceledException)
          {
            break;
          }
        }
      });
    }

    public void Stop()
    {
      cts?.Cancel();
      cts?.Dispose();
      cts = null;
    }


    internal void ResolveCollisions()
    {
      foreach (Ball other in allBalls)
      {
        if (ReferenceEquals(this, other))
          continue;
        if (allBalls.IndexOf(this) > allBalls.IndexOf(other))
          continue;

        double dx = other.position.x - position.x;
        double dy = other.position.y - position.y;
        double distanceSq = dx * dx + dy * dy;
        double radii = Diameter; // suma promieni = 10 + 10

        if (distanceSq < radii * radii)
        {
          double distance = Math.Sqrt(distanceSq);

          if (distance == 0)
            continue;

          double nx = dx / distance;
          double ny = dy / distance;

          double rvx = other.Velocity.x - Velocity.x;
          double rvy = other.Velocity.y - Velocity.y;

          double velAlongNormal = rvx * nx + rvy * ny;

          // jeśli się oddalają - ignoruj
          if (velAlongNormal > 0)
            continue;

          double restitution = 1;

          double impulse = -(1 + restitution) * velAlongNormal / 2.0;

          double impulseX = impulse * nx;
          double impulseY = impulse * ny;

          Velocity = new Vector(
              Velocity.x - impulseX,
              Velocity.y - impulseY);

          other.Velocity = new Vector(
              other.Velocity.x + impulseX,
              other.Velocity.y + impulseY);


          double normal = Math.Sqrt(dx * dx + dy * dy);

          double penetration = radii - distance;

          position = new Vector(position.x - penetration * (dx / normal) * 0.5, position.y - penetration * (dy / normal) * 0.5);
          other.position = new Vector(other.position.x + penetration * (dx / normal) * 0.5, other.position.y + penetration * (dy / normal) * 0.5);
        }
      }
    }




    internal void MoveBall(double width, double height)
    {
      double deltaX = Velocity.x;
      double deltaY = Velocity.y;
      double nextX = Position.x + deltaX;
      double nextY = Position.y + deltaY;
      double maxX = width - Diameter - 4 * 2;
      double maxY = height - Diameter - 4 * 2;

      if (nextX < 0 || nextX > maxX)
      {
        deltaX = -deltaX;
        Velocity = new Vector(deltaX, deltaY);
        nextX = Position.x + deltaX;
      }
      if (nextY < 0 || nextY > maxY)
      {
        deltaY = -deltaY;
        Velocity = new Vector(deltaX, deltaY);
        nextY = Position.y + deltaY;
      }

      Move(new Vector(nextX - Position.x, nextY - Position.y));
    }

    internal void ValidateVelocity(double width, double height)
    {
      double maxX = width - Diameter - 4 * 2;
      double maxY = height - Diameter - 4 * 2;

      if (Math.Abs(Velocity.x) > maxX || Math.Abs(Velocity.y) > maxY)
      {
        throw new ArgumentOutOfRangeException(
          nameof(Velocity),
          "Velocity cannot be greater than available table area.");
      }
    }

    private void RaiseNewPositionChangeNotification()
    {
      NewPositionNotification?.Invoke(this, position);
    }

    internal void Move(Vector delta)
    {
      position = new Vector(position.x + delta.x, position.y + delta.y);
      RaiseNewPositionChangeNotification();
    }

    #endregion
  }
}