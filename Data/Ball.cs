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
      ValidateVelocity(width, height);

      cts = new CancellationTokenSource();
      CancellationToken token = cts.Token;

      Task.Run(async () =>
      {
        Stopwatch sw = Stopwatch.StartNew();

        int tickNumber = 0;

        long previousTickMs = sw.ElapsedMilliseconds - TargetIntervalMs;

        while (!token.IsCancellationRequested)
        {
          tickNumber++;

          long expectedMs = (long)tickNumber * TargetIntervalMs;

          long currentTickMs = sw.ElapsedMilliseconds;
          double elapsedTimeMs = currentTickMs - previousTickMs;
          previousTickMs = currentTickMs;

          elapsedTimeMs = Math.Clamp(elapsedTimeMs, 1.0, MaxCumulativeLatencyMs);

          MoveBall(width, height, elapsedTimeMs);
          ResolveCollisions();

          long nowMs = sw.ElapsedMilliseconds;
          long waitMs = expectedMs - nowMs;

          long latency = nowMs - expectedMs;
          if (latency > MaxCumulativeLatencyMs)
          {
            sw.Restart();
            tickNumber = 0;
            previousTickMs = sw.ElapsedMilliseconds - TargetIntervalMs;
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
        double radii = Diameter;

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
      MoveBall(width, height, TargetIntervalMs);
    }

    internal void MoveBall(double width, double height, double elapsedTimeMs)
    {
      double movementScale = elapsedTimeMs / TargetIntervalMs;

      double deltaX = Velocity.x * movementScale;
      double deltaY = Velocity.y * movementScale;

      double nextX = Position.x + deltaX;
      double nextY = Position.y + deltaY;

      if (nextX <= 0 || nextX >= width - Diameter - 4 * 2)
      {
        Velocity = new Vector(-Velocity.x, Velocity.y);
        deltaX = -deltaX;
      }

      if (nextY <= 0 || nextY >= height - Diameter - 4 * 2)
      {
        Velocity = new Vector(Velocity.x, -Velocity.y);
        deltaY = -deltaY;
      }

      Move(new Vector(deltaX, deltaY));
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