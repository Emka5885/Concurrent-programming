//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class Ball : IBall
  {
    public Ball(Data.IBall ball)
    {
      this.ball = ball;
    }

    #region IBall

    public event EventHandler<IPosition>? NewPositionNotification
    {
      add
      {
        bool hadNoSubscribers = newPositionNotification == null;

        newPositionNotification += value;

        if (hadNoSubscribers)
          ball.NewPositionNotification += RaisePositionChangeEvent;
      }
      remove
      {
        newPositionNotification -= value;

        if (newPositionNotification == null)
          ball.NewPositionNotification -= RaisePositionChangeEvent;
      }
    }

    public IPosition Position
    {
      get
      {
        Data.IVector currentPosition = ball.Position;
        return new Position(currentPosition.x, currentPosition.y);
      }
    }

    public double Diameter => ball.Diameter;

    #endregion IBall

    #region private

    private readonly Data.IBall ball;
    private EventHandler<IPosition>? newPositionNotification;

    private void RaisePositionChangeEvent(object? sender, Data.IVector e)
    {
      newPositionNotification?.Invoke(this, new Position(e.x, e.y));
    }

    #endregion private
  }
}