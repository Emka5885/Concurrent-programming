//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
  public class MainWindowViewModel : ViewModelBase, IDisposable
  {
    #region ctor

    public MainWindowViewModel() : this(null)
    { }

    internal MainWindowViewModel(ModelAbstractApi modelLayerAPI)
    {
      ModelLayer = modelLayerAPI == null ? ModelAbstractApi.CreateModel() : modelLayerAPI;
      Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));
      StartBallsCommand = new RelayCommand(() => Start(BallCount), () => !Disposed && !Started && BallCount > 0);
    }

    #endregion ctor

    #region public API

    public void Start(int numberOfBalls)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(MainWindowViewModel));

      if (Started || numberOfBalls <= 0)
        return;

      Balls.Clear();
      ModelLayer.Start(numberOfBalls);
      Started = true;
      StartBallsCommand.RaiseCanExecuteChanged();
    }

    public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();

    public int BallCount
    {
      get => ballCount;
      set
      {
        if (ballCount == value)
          return;

        ballCount = value;
        RaisePropertyChanged();
        StartBallsCommand.RaiseCanExecuteChanged();
      }
    }

    public RelayCommand StartCommand => StartBallsCommand;

    #endregion public API

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          Balls.Clear();
          Observer.Dispose();
          ModelLayer.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        Disposed = true;
      }
    }

    public void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(MainWindowViewModel));
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable

    #region private

    private IDisposable Observer = null;
    private ModelAbstractApi ModelLayer;
    private bool Disposed = false;
    private readonly RelayCommand StartBallsCommand;
    private int ballCount = 8;
    private bool Started = false;

    public double Width => ModelLayer.Width;
    public double Height => ModelLayer.Height;

    #endregion private
  }
}