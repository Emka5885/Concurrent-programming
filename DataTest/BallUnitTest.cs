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
      Ball newInstance = new(testingVector, testingVector, 20.0, null, null);

      Assert.IsNotNull(newInstance);
    }

    [TestMethod]
    public void MoveTestMethod() // czy po ruchu kulki wywołuje się event ?
    {
      Vector initialPosition = new(10.0, 10.0);
      Ball newInstance = new(initialPosition, new Vector(0.0, 0.0), 20.0, null, null);
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
      Ball ball = new Ball(new Vector(10.0, 20.0), new Vector(0.0, 0.0), 20.0, null, null);

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
      Ball ball = new Ball(new Vector(5.0, 5.0), new Vector(0.0, 0.0), 20.0, null, null);

      object? capturedSender = null;
      ball.NewPositionNotification += (sender, _) => capturedSender = sender;

      ball.Move(new Vector(1.0, 1.0));

      Assert.IsNotNull(capturedSender);
      Assert.AreSame(ball, capturedSender);
    }

    [TestMethod]
    public void StartThrowsExceptionWhenVelocityIsGreaterThanTableSize() // prędkość w 1 kroku, nie może być większa niż rozmiar planszy
    {
      double tableWidth = 100.0;
      double tableHeight = 100.0;

      Ball ball = new Ball(
        new Vector(10.0, 10.0),
        new Vector(150.0, 20.0),
        20.0, null, null);

      Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
      {
        ball.ValidateVelocity(tableWidth, tableHeight);
      });
    }

    [TestMethod]
    public void CollisionExchangesVelocityBetweenTwoBalls()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      Ball ballA = new Ball(
          new Vector(0.0, 0.0),
          new Vector(1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      Ball ballB = new Ball(
          new Vector(19.0, 0.0),
          new Vector(-1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      balls.Add(ballA);
      balls.Add(ballB);

      ballA.ResolveCollisions();

      Assert.AreEqual(-1.0, ballA.Velocity.x, 1e-10);
      Assert.AreEqual(1.0, ballB.Velocity.x, 1e-10);
    }

    [TestMethod]
    public void BallsDoNotChangeVelocityWhenTheyDoNotCollide()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      Ball ballA = new Ball(
          new Vector(0.0, 0.0),
          new Vector(1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      Ball ballB = new Ball(
          new Vector(100.0, 0.0),
          new Vector(-1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      balls.Add(ballA);
      balls.Add(ballB);

      ballA.ResolveCollisions();

      Assert.AreEqual(1.0, ballA.Velocity.x, 1e-10);
      Assert.AreEqual(-1.0, ballB.Velocity.x, 1e-10);
    }

    [TestMethod]
    public void ThreeBallCollisionTransfersVelocity()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      Ball ball1 = new Ball(
          new Vector(0.0, 0.0),
          new Vector(1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      Ball ball2 = new Ball(
          new Vector(19.0, 0.0),
          new Vector(0.0, 0.0),
          20.0,
          balls,
          physicsLock);

      Ball ball3 = new Ball(
          new Vector(38.0, 0.0),
          new Vector(0.0, 0.0),
          20.0,
          balls,
          physicsLock);

      balls.Add(ball1);
      balls.Add(ball2);
      balls.Add(ball3);

      for (int i = 0; i < 5; i++)
      {
        foreach (Ball ball in balls)
        {
          ball.ResolveCollisions();
        }
      }

      Assert.IsTrue(ball3.Velocity.x > 0.5);
      Assert.IsTrue(Math.Abs(ball1.Velocity.x) < 0.5);
    }


    [TestMethod]
    public void CollisionDoesNotIncreaseTotalEnergy()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      Ball ballA = new Ball(
          new Vector(0.0, 0.0),
          new Vector(1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      Ball ballB = new Ball(
          new Vector(19.0, 0.0),
          new Vector(-1.0, 0.0),
          20.0,
          balls,
          physicsLock);

      balls.Add(ballA);
      balls.Add(ballB);

      double energyBefore =
          ballA.Velocity.x * ballA.Velocity.x +
          ballB.Velocity.x * ballB.Velocity.x;

      ballA.ResolveCollisions();

      double energyAfter =
          ballA.Velocity.x * ballA.Velocity.x +
          ballB.Velocity.x * ballB.Velocity.x;

      Assert.AreEqual(energyBefore, energyAfter, 1e-10);
    }
  }
}