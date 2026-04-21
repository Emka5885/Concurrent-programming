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
    #region ctor

    public DataImplementation()
    {
      MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    #endregion ctor

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
        Vector startingPosition = new(random.Next(100, 400 - 100), random.Next(100, 400 - 100));
     
        double velocityX = (random.NextDouble() - 0.5) * 10;
        double velocityY = (random.NextDouble() - 0.5) * 10;

        if (Math.Abs(velocityX) < 0.1) velocityX = 2.0;
        if (Math.Abs(velocityY) < 0.1) velocityY = 2.0;

        Vector startingVelocity = new(velocityX, velocityY);

        Ball newBall = new(startingPosition, startingVelocity, 20);
        upperLayerHandler(startingPosition, newBall);
        BallsList.Add(newBall);
      }
    }
    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          MoveTimer.Dispose();
          BallsList.Clear();
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

    //private bool disposedValue;
    private bool Disposed = false;

    private readonly Timer MoveTimer;
    private Random RandomGenerator = new();
    private List<Ball> BallsList = [];

    public override double Width { get; } = 420;
    public override double Height { get; } = 400;


    private void Move(object? x)
    {
      foreach (Ball item in BallsList)
      {
        double deltaX = item.Velocity.x;
        double deltaY = item.Velocity.y;

        double nextX = item.Position.x + deltaX;
        double nextY = item.Position.y + deltaY;

        double maxX = Width - item.Diameter - 4 * 2;
        double maxY = Height - item.Diameter - 4 * 2;

        if (nextX < 0 || nextX > maxX)
        {
          deltaX = -deltaX;
          item.Velocity = new Vector(deltaX, deltaY);
          nextX = item.Position.x + deltaX;
        }

        if (nextY < 0 || nextY > maxY)
        {
          deltaY = -deltaY;
          item.Velocity = new Vector(deltaX, deltaY);
          nextY = item.Position.y + deltaY;
        }

        double newDeltaX = nextX - item.Position.x;
        double newDeltaY = nextY - item.Position.y;

        item.Move(new Vector(newDeltaX, newDeltaY));
      }
    }

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