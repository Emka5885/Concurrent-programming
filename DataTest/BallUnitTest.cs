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
    public async Task SimulationShouldNotDeadlock()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      for (int i = 0; i < 20; i++)
      {
        Ball ball = new Ball(
            new Vector(i * 10.0, 0.0),
            new Vector(1.0, 0.0),
            20.0,
            balls,
            physicsLock);

        balls.Add(ball);
      }

      foreach (Ball ball in balls)
      {
        ball.Start(500, 500);
      }

      await Task.Delay(1000);

      bool allBallsStillMoving =
          balls.All(ball => Math.Abs(ball.Velocity.x) > 0);

      foreach (Ball ball in balls)
      {
        ball.Stop();
      }

      Assert.IsTrue(allBallsStillMoving);
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

      foreach (Ball ball in balls)
      {
        ball.ResolveCollisions();
      }

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



    [TestMethod]
    public void CollisionConservesMomentum()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      Ball ballA = new Ball(
          new Vector(0, 0),
          new Vector(1, 0),
          20,
          balls,
          physicsLock);

      Ball ballB = new Ball(
          new Vector(19, 0),
          new Vector(-1, 0),
          20,
          balls,
          physicsLock);

      balls.Add(ballA);
      balls.Add(ballB);

      double momentumBefore =
          ballA.Velocity.x + ballB.Velocity.x;

      ballA.ResolveCollisions();

      double momentumAfter =
          ballA.Velocity.x + ballB.Velocity.x;

      Assert.AreEqual(momentumBefore, momentumAfter, 1e-10);
    }



    [TestMethod]
    [Timeout(15000)]
    public async Task EachBallExecutesApproximatelyEqualNumberOfTimesOver10Seconds()
    {
      const int ballCount = 5;
      object physicsLock = new();
      List<Ball> balls = new();
      int[] tickCounts = new int[ballCount];
      int countingEnabled = 0;

      for (int i = 0; i < ballCount; i++)
      {
        int index = i;

        Ball ball = new Ball(
          new Vector(i * 50.0, 50.0),
          new Vector(1.0, 0.0),
          20.0,
          balls,
          physicsLock);

        ball.NewPositionNotification += (_, _) =>
        {
          if (Volatile.Read(ref countingEnabled) == 1)
          {
            Interlocked.Increment(ref tickCounts[index]);
          }
        };

        balls.Add(ball);
      }

      try
      {
        foreach (Ball ball in balls)
        {
          ball.Start(420, 400);
        }

        Volatile.Write(ref countingEnabled, 1);

        await Task.Delay(TimeSpan.FromSeconds(10));

        Volatile.Write(ref countingEnabled, 0);
      }
      finally
      {
        foreach (Ball ball in balls)
        {
          ball.Stop();
        }
      }

      int min = tickCounts.Min();
      int max = tickCounts.Max();

      Assert.IsTrue(
        min > 0,
        $"Co najmniej jedna kulka nie wykonała się ani razu. Counts: {string.Join(", ", tickCounts)}");

      Assert.IsTrue(
        max - min <= 1,
        $"Kulki nie wykonały się równomiernie. Counts: {string.Join(", ", tickCounts)}, min={min}, max={max}.");
    }

    [TestMethod]
    public void ThreeBallCollisionTransfersVelocity() // 3 balls in one line
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
    public void ThreeBallsTouchingEachOtherAtTheSameTime()
    {
      object physicsLock = new();
      List<Ball> balls = new();

      const double diameter = 20.0;
      const double side = 19.0;
      double height = side * Math.Sqrt(3.0) / 2.0;

      Ball ball1 = CreateBall(0.0, 0.0, 0.6, 0.35, diameter, balls, physicsLock);
      Ball ball2 = CreateBall(side, 0.0, -0.6, 0.35, diameter, balls, physicsLock);
      Ball ball3 = CreateBall(side / 2.0, height, 0.0, -0.7, diameter, balls, physicsLock);

      balls.AddRange([ball1, ball2, ball3]);

      Assert.IsTrue(AreBallsOverlapping(ball1, ball2));
      Assert.IsTrue(AreBallsOverlapping(ball1, ball3));
      Assert.IsTrue(AreBallsOverlapping(ball2, ball3));

      double energyBefore = CalculateTotalEnergy(balls);

      ResolveAllCollisions(balls, 5);

      double energyAfter = CalculateTotalEnergy(balls);

      Assert.IsTrue(AreAllVelocitiesValid(balls));
      Assert.AreEqual(energyBefore, energyAfter, 1e-6);
    }

    private static Ball CreateBall(
    double x,
    double y,
    double velocityX,
    double velocityY,
    double diameter,
    List<Ball> balls,
    object physicsLock)
    {
      return new Ball(
        new Vector(x, y),
        new Vector(velocityX, velocityY),
        diameter,
        balls,
        physicsLock);
    }

    private static void ResolveAllCollisions(List<Ball> balls, int numberOfIterations)
    {
      for (int i = 0; i < numberOfIterations; i++)
      {
        foreach (Ball ball in balls)
        {
          ball.ResolveCollisions();
        }
      }
    }

    private static bool AreAllVelocitiesValid(IEnumerable<Ball> balls)
    {
      return balls.All(ball =>
        !double.IsNaN(ball.Velocity.x) &&
        !double.IsNaN(ball.Velocity.y));
    }

    private static bool AreBallsOverlapping(Ball firstBall, Ball secondBall)
    {
      double dx = secondBall.Position.x - firstBall.Position.x;
      double dy = secondBall.Position.y - firstBall.Position.y;

      double distanceSquared = dx * dx + dy * dy;
      double minimumDistance = firstBall.Diameter / 2.0 + secondBall.Diameter / 2.0;

      return distanceSquared < minimumDistance * minimumDistance;
    }

    private static double CalculateTotalEnergy(IEnumerable<Ball> balls)
    {
      return balls.Sum(ball =>
      {
        double vx = ball.Velocity.x;
        double vy = ball.Velocity.y;

        return 0.5 * (vx * vx + vy * vy);
      });
    }
  }
}