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

    private Vector position;
    private readonly double diameter;

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