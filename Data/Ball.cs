using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(
      Vector initialPosition,
      Vector initialVelocity,
      double diameter,
      List<Ball> allBalls,
      object physicLock,
      bool isControlledByUser = false)
    {
      position = initialPosition;
      Velocity = initialVelocity;
      this.diameter = diameter;
      this.allBalls = allBalls;
      this.physicLock = physicLock;
      IsControlledByUser = isControlledByUser;
    }

    #endregion

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;
    public IVector Velocity { get; set; }
    public IVector Position => position;
    public double Diameter => diameter;

    internal bool IsControlledByUser { get; }

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
      if (IsControlledByUser)
        return;

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
      lock (physicLock)
      {
        foreach (Ball other in allBalls)
        {
          if (ReferenceEquals(this, other))
            continue;

          if (IsControlledByUser || other.IsControlledByUser)
          {
            ResolveCollisionWithControlledBall(this, other);
            continue;
          }
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
    }


    private static void ResolveCollisionWithControlledBall(Ball firstBall, Ball secondBall)
    {
      Ball controlledBall = firstBall.IsControlledByUser ? firstBall : secondBall;
      Ball normalBall = firstBall.IsControlledByUser ? secondBall : firstBall;

      double controlledRadius = controlledBall.Diameter / 2.0;
      double normalRadius = normalBall.Diameter / 2.0;

      double controlledCenterX = controlledBall.Position.x + controlledRadius;
      double controlledCenterY = controlledBall.Position.y + controlledRadius;

      double normalCenterX = normalBall.Position.x + normalRadius;
      double normalCenterY = normalBall.Position.y + normalRadius;

      double dx = normalCenterX - controlledCenterX;
      double dy = normalCenterY - controlledCenterY;

      double distanceSquared = dx * dx + dy * dy;
      double minimumDistance = controlledRadius + normalRadius;

      if (distanceSquared >= minimumDistance * minimumDistance)
        return;

      double distance = Math.Sqrt(distanceSquared);

      if (distance == 0.0)
      {
        dx = 1.0;
        dy = 0.0;
        distance = 1.0;
      }

      double normalX = dx / distance;
      double normalY = dy / distance;

      double penetration = minimumDistance - distance;

      normalBall.Move(new Vector(
        normalX * penetration,
        normalY * penetration));

      double relativeVelocityX = normalBall.Velocity.x - controlledBall.Velocity.x;
      double relativeVelocityY = normalBall.Velocity.y - controlledBall.Velocity.y;

      double velocityAlongNormal =
        relativeVelocityX * normalX + relativeVelocityY * normalY;

      if (velocityAlongNormal >= 0.0)
        return;

      double reflectedRelativeVelocityX =
        relativeVelocityX - 2.0 * velocityAlongNormal * normalX;

      double reflectedRelativeVelocityY =
        relativeVelocityY - 2.0 * velocityAlongNormal * normalY;

      const double controlledBallImpactFactor = 0.45;

      normalBall.Velocity = new Vector(
        controlledBall.Velocity.x * controlledBallImpactFactor + reflectedRelativeVelocityX,
        controlledBall.Velocity.y * controlledBallImpactFactor + reflectedRelativeVelocityY);
    }


    internal void SetPosition(Vector newPosition)
    {
      position = newPosition;
      RaiseNewPositionChangeNotification();
    }

    internal void SetControlledPosition(Vector newPosition, double elapsedTimeMs)
    {
      double safeElapsedTimeMs = Math.Max(1.0, elapsedTimeMs);
      double velocityScale = TargetIntervalMs / safeElapsedTimeMs;

      double velocityX = (newPosition.x - Position.x) * velocityScale;
      double velocityY = (newPosition.y - Position.y) * velocityScale;

      const double maxControlledVelocity = 6.0;

      double speed = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);

      if (speed > maxControlledVelocity)
      {
        double scale = maxControlledVelocity / speed;

        velocityX *= scale;
        velocityY *= scale;
      }

      Velocity = new Vector(velocityX, velocityY);

      position = newPosition;

      RaiseNewPositionChangeNotification();
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