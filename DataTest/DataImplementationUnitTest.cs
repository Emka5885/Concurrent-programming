//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Collections.Generic;

namespace TP.ConcurrentProgramming.Data.Test
{
  [TestClass]
  public class DataImplementationUnitTest
  {
    [TestMethod]
    public void ConstructorTestMethod() // czy lista kulek istnieje i jest pusta na start ?
    {
      using (DataImplementation newInstance = new DataImplementation())
      {
        IEnumerable<IBall>? ballsList = null;
        newInstance.CheckBallsList(x => ballsList = x);
        Assert.IsNotNull(ballsList);

        int numberOfBalls = 0;
        newInstance.CheckNumberOfBalls(x => numberOfBalls = x);
        Assert.AreEqual(0, numberOfBalls);
      }
    }

    [TestMethod]
    public void DisposeTestMethod() // czy obiekt się poprawnie zamyka ?   czy nie można go używać po Dispose() ?
    {
      DataImplementation newInstance = new DataImplementation();

      bool newInstanceDisposed = false;
      newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
      Assert.IsFalse(newInstanceDisposed);

      newInstance.Dispose();

      newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
      Assert.IsTrue(newInstanceDisposed);

      IEnumerable<IBall>? ballsList = null;
      newInstance.CheckBallsList(x => ballsList = x);
      Assert.IsNotNull(ballsList);

      newInstance.CheckNumberOfBalls(x => Assert.AreEqual(0, x));

      Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Dispose());
      Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Start(0, (position, ball) => { }));
    }

    [TestMethod]
    public void StartTestMethod() // czy tworzy się poprawna liczba kulek ?   czy callback jest wywoływany ?
    {
      using (DataImplementation newInstance = new DataImplementation())
      {
        int numberOfCallbackInvoked = 0;
        int numberOfBalls2Create = 10;

        newInstance.Start(
          numberOfBalls2Create,
          (startingPosition, ball) =>
          {
            numberOfCallbackInvoked++;
            Assert.IsTrue(startingPosition.x >= 0);
            Assert.IsTrue(startingPosition.y >= 0);
            Assert.IsNotNull(ball);
          });

        Assert.AreEqual(numberOfBalls2Create, numberOfCallbackInvoked);
        newInstance.CheckNumberOfBalls(x => Assert.AreEqual(10, x));
      }
    }

    [TestMethod]
    public void StartingPositionsWithinUpperBounds() // ! czy kulki startują w środku planszy ?
    {
      using DataImplementation impl = new DataImplementation();

      const double diameter = 20.0;
      const double border = 4.0 * 2;
      double maxX = impl.Width - diameter - border;
      double maxY = impl.Height - diameter - border;

      impl.Start(30, (pos, _) =>
      {
        Assert.IsTrue(pos.x >= 0 && pos.x <= maxX,
          $"Startowa pozycja X={pos.x:F2} poza obszarem [0..{maxX:F2}]");
        Assert.IsTrue(pos.y >= 0 && pos.y <= maxY,
          $"Startowa pozycja Y={pos.y:F2} poza obszarem [0..{maxY:F2}]");
      });
    }

    [TestMethod]
    public void StartCreatesExactlyNBalls() // ! czy nie dodajemy kulek podwójnie ?
    {
      using DataImplementation impl = new DataImplementation();
      const int n = 5;

      impl.Start(n, (_, __) => { });

      impl.CheckNumberOfBalls(count =>
        Assert.AreEqual(n, count,
          $"Lista zawiera {count} kulek zamiast {n}."));
    }

    [TestMethod]
    public void StartWithZeroBallsCreatesEmptyList() // ! czy 0 kulek stworzy pustą listę ?
    {
      using DataImplementation impl = new DataImplementation();

      impl.Start(0, (_, __) =>
        Assert.Fail("Callback nie powinien być wywołany dla 0 kulek."));

      impl.CheckNumberOfBalls(count =>
        Assert.AreEqual(0, count));
    }

    [TestMethod]
    public void StartThrowsWhenHandlerIsNull() // czy jak null to wyjątek ?
    {
      using DataImplementation impl = new DataImplementation();

      Assert.ThrowsException<ArgumentNullException>(
        () => impl.Start(5, null!));
    }
  }
}