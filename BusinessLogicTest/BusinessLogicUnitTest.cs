//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using TP.ConcurrentProgramming.Data;

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
  [TestClass]
  public class BusinessLogicImplementationUnitTest
  {
    [TestMethod]
    public void ConstructorTestMethod() // czy obiekt BL startuje w poprawnym stanie ?
    {
      using (BusinessLogicImplementation newInstance = new(new DataLayerConstructorFixture()))
      {
        bool newInstanceDisposed = true;
        newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
        Assert.IsFalse(newInstanceDisposed);
      }
    }

    [TestMethod]
    public void DisposeTestMethod() // czy logic i data się zamykają ?
    {
      DataLayerDisposeFixture dataLayerFixture = new DataLayerDisposeFixture();
      BusinessLogicImplementation newInstance = new(dataLayerFixture);

      Assert.IsFalse(dataLayerFixture.Disposed);

      bool newInstanceDisposed = true;
      newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
      Assert.IsFalse(newInstanceDisposed);

      newInstance.Dispose();

      newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
      Assert.IsTrue(newInstanceDisposed);

      Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Dispose());
      Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Start(0, (position, ball) => { }));
      Assert.IsTrue(dataLayerFixture.Disposed);
    }

    [TestMethod]
    public void StartTestMethod() // czy logic wywołuje Data.Start i przekazuje liczbę kulek ?
    {
      DataLayerStartFixture dataLayerFixture = new();

      using (BusinessLogicImplementation newInstance = new(dataLayerFixture))
      {
        int called = 0;
        int numberOfBalls2Create = 10;

        newInstance.Start(
          numberOfBalls2Create,
          (startingPosition, ball) =>
          {
            called++;
            Assert.IsNotNull(startingPosition);
            Assert.IsNotNull(ball);
          });

        Assert.AreEqual(1, called);
        Assert.IsTrue(dataLayerFixture.StartCalled);
        Assert.AreEqual(numberOfBalls2Create, dataLayerFixture.NumberOfBallsCreated);
      }
    }

    [TestMethod]
    public void StartSubscribesToDataBallPositionEvents() // czy Logic nasłuchuje eventów z Data ?
    {
      DataLayerEventFixture dataLayer = new DataLayerEventFixture();
      using BusinessLogicImplementation logic = new(dataLayer);

      int positionEventsReceived = 0;
      IPosition? lastReceivedPosition = null;

      logic.Start(1, (_, ball) =>
      {
        ball.NewPositionNotification += (_, pos) =>
        {
          positionEventsReceived++;
          lastReceivedPosition = pos;
        };
      });

      dataLayer.FirePositionOnAllBalls(7.5, 12.3);

      Assert.AreEqual(1, positionEventsReceived);
      Assert.IsNotNull(lastReceivedPosition);
      Assert.AreEqual(7.5, lastReceivedPosition.x, 1e-10);
      Assert.AreEqual(12.3, lastReceivedPosition.y, 1e-10);
    }

    [TestMethod]
    public void StartCallbackReceivesBusinessLogicPositionNotDataVector() // czy Presentation dostaje typ BL, nie Data ?
    {
      DataLayerStartFixture dataLayer = new DataLayerStartFixture();
      using BusinessLogicImplementation logic = new(dataLayer);

      IPosition? receivedPosition = null;
      IBall? receivedBall = null;

      logic.Start(1, (pos, ball) =>
      {
        receivedPosition = pos;
        receivedBall = ball;
      });

      Assert.IsNotNull(receivedPosition);
      Assert.IsNotNull(receivedBall);
      Assert.IsInstanceOfType(receivedPosition, typeof(IPosition));
      Assert.IsFalse(receivedPosition is Data.IVector);
      Assert.IsInstanceOfType(receivedBall, typeof(IBall));
      Assert.IsFalse(receivedBall is Data.IBall);
    }

    [TestMethod]
    public void StartMapsXAndYCoordinatesCorrectly() // czy x i y są poprawnie przepisane ?
    {
      DataLayerCoordinateFixture dataLayer = new DataLayerCoordinateFixture();
      using BusinessLogicImplementation logic = new(dataLayer);

      IPosition? receivedPosition = null;

      logic.Start(1, (pos, _) => receivedPosition = pos);

      Assert.IsNotNull(receivedPosition);
      Assert.AreEqual(11.0, receivedPosition.x, 1e-10);
      Assert.AreEqual(22.0, receivedPosition.y, 1e-10);
    }

    [TestMethod]
    public void StartPassesExactBallCountToDataLayer() // czy liczba kulek nie zmienia się po drodze ?
    {
      DataLayerStartFixture dataLayer = new();
      using BusinessLogicImplementation logic = new(dataLayer);

      logic.Start(42, (_, __) => { });

      Assert.AreEqual(42, dataLayer.NumberOfBallsCreated);
    }

    private class DataLayerConstructorFixture : Data.DataAbstractAPI
    {
      public override double Width => 400.0;
      public override double Height => 420.0;

      public override void Dispose() { }

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> upperLayerHandler)
        => throw new NotImplementedException();
    }

    private class DataLayerDisposeFixture : Data.DataAbstractAPI
    {
      public override double Width => 400.0;
      public override double Height => 420.0;

      internal bool Disposed = false;

      public override void Dispose() => Disposed = true;

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> upperLayerHandler)
        => throw new NotImplementedException();
    }

    private class DataLayerStartFixture : Data.DataAbstractAPI
    {
      public override double Width => 400.0;
      public override double Height => 420.0;

      internal bool StartCalled = false;
      internal int NumberOfBallsCreated = -1;

      public override void Dispose() { }

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> upperLayerHandler)
      {
        StartCalled = true;
        NumberOfBallsCreated = numberOfBalls;
        upperLayerHandler(new VectorFixture(0.0, 0.0), new DataBallFixture());
      }
    }

    private class DataLayerCoordinateFixture : Data.DataAbstractAPI
    {
      public override double Width => 400.0;
      public override double Height => 420.0;

      public override void Dispose() { }

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> upperLayerHandler)
      {
        upperLayerHandler(new VectorFixture(11.0, 22.0), new DataBallFixture());
      }
    }

    private class DataLayerEventFixture : Data.DataAbstractAPI
    {
      public override double Width => 400.0;
      public override double Height => 420.0;

      private readonly List<DataBallFixture> balls = new();

      public override void Dispose() { }

      public override void Start(int numberOfBalls, Action<IVector, Data.IBall> upperLayerHandler)
      {
        for (int i = 0; i < numberOfBalls; i++)
        {
          DataBallFixture ball = new DataBallFixture();
          balls.Add(ball);
          upperLayerHandler(new VectorFixture(0.0, 0.0), ball);
        }
      }

      internal void FirePositionOnAllBalls(double x, double y)
      {
        foreach (DataBallFixture ball in balls)
          ball.FirePosition(x, y);
      }
    }

    private class DataBallFixture : Data.IBall
    {
      public IVector Velocity
      {
        get => new VectorFixture(0.0, 0.0);
        set { }
      }

      public double Diameter => 20.0;

      public event EventHandler<IVector>? NewPositionNotification;

      internal void FirePosition(double x, double y)
      {
        NewPositionNotification?.Invoke(this, new VectorFixture(x, y));
      }
    }

    private class VectorFixture : Data.IVector
    {
      internal VectorFixture(double X, double Y)
      {
        x = X;
        y = Y;
      }

      public double x { get; init; }
      public double y { get; init; }
    }
  }
}