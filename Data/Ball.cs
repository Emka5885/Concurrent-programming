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
      velocity = initialVelocity;
      this.diameter = diameter;
    }

    #endregion ctor

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    private Vector velocity;

    public IVector Velocity
    {
      get
      {
        lock (ballLock)
        {
          return velocity;
        }
      }
      set
      {
        lock (ballLock)
        {
          velocity = new Vector(value.x, value.y);
        }
      }
    }

    public IVector Position
    {
      get
      {
        lock (ballLock)
        {
          return position;
        }
      }
    }
    public double Diameter => diameter;

    #endregion IBall

    #region private

    private Vector position;
    private readonly double diameter;

    private readonly object ballLock = new object();

    private void ValidateVelocity(double width, double height)
    {
      double maxX = width - Diameter - 4 * 2;
      double maxY = height - Diameter - 4 * 2;

      IVector currentVelocity = Velocity;

      if (Math.Abs(currentVelocity.x) > maxX || Math.Abs(currentVelocity.y) > maxY)
      {
        throw new ArgumentOutOfRangeException(
          nameof(Velocity),
          "Velocity cannot be greater than available table area.");
      }
    }

    internal void MoveBall(double width, double height)
    {
      ValidateVelocity(width, height);

      Vector newPosition;

      lock (ballLock)
      {
        double deltaX = velocity.x;
        double deltaY = velocity.y;

        double nextX = position.x + deltaX;
        double nextY = position.y + deltaY;

        double maxX = width - Diameter - 4 * 2;
        double maxY = height - Diameter - 4 * 2;

        if (nextX < 0 || nextX > maxX)
        {
          deltaX = -deltaX;
          nextX = position.x + deltaX;
        }

        if (nextY < 0 || nextY > maxY)
        {
          deltaY = -deltaY;
          nextY = position.y + deltaY;
        }

        velocity = new Vector(deltaX, deltaY);
        position = new Vector(nextX, nextY);

        newPosition = position;
      }

      RaiseNewPositionChangeNotification(newPosition);
    }


    private void RaiseNewPositionChangeNotification(IVector newPosition)
    {
      NewPositionNotification?.Invoke(this, newPosition);
    }

    internal void Move(Vector delta)
    {
      Vector newPosition;

      lock (ballLock)
      {
        position = new Vector(position.x + delta.x, position.y + delta.y);
        newPosition = position;
      }

      RaiseNewPositionChangeNotification(newPosition);
    }

    #endregion private
  }
}