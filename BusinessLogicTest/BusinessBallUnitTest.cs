//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.BusinessLogic.Test
{
  [TestClass]
  public class BallUnitTest
  {
    [TestMethod]
    public void MoveTestMethod()
    {
      DataBallFixture dataBallFixture = new DataBallFixture();
      Ball newInstance = new(dataBallFixture);
      int numberOfCallBackCalled = 0;

      newInstance.NewPositionNotification += (sender, position) =>
      {
        Assert.IsNotNull(sender);
        Assert.IsNotNull(position);
        numberOfCallBackCalled++;
      };

      dataBallFixture.Fire(0.0, 0.0);

      Assert.AreEqual(1, numberOfCallBackCalled);
    }

    [TestMethod]
    public void PositionNotificationTranslatesDataVectorToBusinessLogicPosition()
    {
      DataBallFixture dataBall = new DataBallFixture();
      Ball businessBall = new Ball(dataBall);

      IPosition? receivedPosition = null;
      businessBall.NewPositionNotification += (_, pos) => receivedPosition = pos;

      dataBall.Fire(3.14, 2.71);

      Assert.IsNotNull(receivedPosition);
      Assert.IsInstanceOfType(receivedPosition, typeof(IPosition));
      Assert.IsFalse(receivedPosition is Data.IVector);
      Assert.AreEqual(3.14, receivedPosition.x, 1e-10);
      Assert.AreEqual(2.71, receivedPosition.y, 1e-10);
    }

    [TestMethod]
    public void EventSenderIsBusinessLogicBallNotDataBall()
    {
      DataBallFixture dataBall = new DataBallFixture();
      Ball businessBall = new Ball(dataBall);

      object? capturedSender = null;
      businessBall.NewPositionNotification += (sender, _) => capturedSender = sender;

      dataBall.Fire(0.0, 0.0);

      Assert.IsNotNull(capturedSender);
      Assert.IsInstanceOfType(capturedSender, typeof(IBall));
      Assert.IsFalse(capturedSender is Data.IBall);
      Assert.AreSame(businessBall, capturedSender);
    }

    [TestMethod]
    public void NotificationFiredExactlyOncePerMove()
    {
      DataBallFixture dataBall = new DataBallFixture();
      Ball businessBall = new Ball(dataBall);

      int callCount = 0;
      businessBall.NewPositionNotification += (_, __) => callCount++;

      dataBall.Fire(1.0, 2.0);
      dataBall.Fire(3.0, 4.0);
      dataBall.Fire(5.0, 6.0);

      Assert.AreEqual(3, callCount);
    }

    private class DataBallFixture : Data.IBall
    {
      public Data.IVector Velocity
      {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
      }

      public event EventHandler<Data.IVector>? NewPositionNotification;

      internal void Fire(double x, double y)
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