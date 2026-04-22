//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class BallUnitTest
  {
    [TestMethod]
    public void ConstructorTestMethod() // czy kulka się poprawnie tworzy?
    {
      Vector testingVector = new Vector(0.0, 0.0);
      Ball newInstance = new(testingVector, testingVector, 20.0);

      Assert.IsNotNull(newInstance);
    }

    [TestMethod]
    public void MoveTestMethod() // czy po ruchu kulki wywołuje się event ?
    {
      Vector initialPosition = new(10.0, 10.0);
      Ball newInstance = new(initialPosition, new Vector(0.0, 0.0), 20.0);
      IVector currentPosition = new Vector(0.0, 0.0);
      int numberOfCallBackCalled = 0;

      newInstance.NewPositionNotification += (sender, position) =>
      {
        Assert.IsNotNull(sender);
        currentPosition = position;
        numberOfCallBackCalled++;
      };

      newInstance.Move(new Vector(0.0, 0.0));

      Assert.AreEqual(1, numberOfCallBackCalled);
      Assert.AreEqual(initialPosition, currentPosition);
    }

    [TestMethod]
    public void MoveAccumulatesPositionAcrossMultipleCalls() // ! czy kolejne ruchy sumują się - a nie nadpisują ?
    {
      Ball ball = new Ball(new Vector(10.0, 20.0), new Vector(0.0, 0.0), 20.0);

      IVector? lastPosition = null;
      ball.NewPositionNotification += (_, pos) => lastPosition = pos;

      ball.Move(new Vector(5.0, -3.0));   // (15, 17)
      ball.Move(new Vector(-2.0, 4.0));   // (13, 21)

      Assert.IsNotNull(lastPosition);
      Assert.AreEqual(13.0, lastPosition.x, 1e-10);
      Assert.AreEqual(21.0, lastPosition.y, 1e-10);
    }

    [TestMethod]
    public void MoveNotificationSenderIsTheBallItself() // czy event mówi "to JA jestem senderem" ?
    {
      Ball ball = new Ball(new Vector(5.0, 5.0), new Vector(0.0, 0.0), 20.0);

      object? capturedSender = null;
      ball.NewPositionNotification += (sender, _) => capturedSender = sender;

      ball.Move(new Vector(1.0, 1.0));

      Assert.IsNotNull(capturedSender);
      Assert.AreSame(ball, capturedSender);
    }
  }
}