using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(Vector initialPosition, Vector initialVelocity, double diameter)
    {
      position = initialPosition;
      Velocity = initialVelocity;
      this.diameter = diameter;
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

    private const int TargetIntervalMs = 16;
    private const long MaxCumulativeLatencyMs = 500;

    public void Start(double width, double height)
    {
      cts = new CancellationTokenSource();
      CancellationToken token = cts.Token;

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

          MoveBall(width, height);

          long nowMs = sw.ElapsedMilliseconds;
          long waitMs = expectedMs - nowMs;

          // Łączne spóźnienie względem harmonogramu
          long latency = nowMs - expectedMs;
          if (latency > MaxCumulativeLatencyMs)
          {
            // Narastające opóźnienie przekroczyło próg — resetuj
            sw.Restart();
            tickNumber = 0;
            // Tu można np. wywołać zdarzenie błędu
          }

          int delayMs = (int)Math.Max(1, waitMs);

          try
          {
            // Token powoduje natychmiastowe przerwanie Delay przy Stop()
            await Task.Delay(delayMs, token);
          }
          catch (OperationCanceledException)
          {
            // Normalne zakończenie — wyjdź z pętli
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

    private void MoveBall(double width, double height)
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