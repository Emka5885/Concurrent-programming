//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

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

    #endregion ctor

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Velocity { get; set; }
    public IVector Position => position;
    public double Diameter => diameter;

    #endregion IBall

    #region private

    private bool isRunning = false;
    private Vector position;
    private readonly double diameter;

    public void Start(double width, double height)
    {
      isRunning = true;

      Task.Run(async () =>
      {
        while (isRunning)
        {
          MoveBall(width, height);
          await Task.Delay(30);
        }
      });
    }

    public void Stop()
    {
      isRunning = false;
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

    #endregion private
  }
}