﻿#region File Info Header
/*________________________________________________________________________________________

  Copyright (C) 2011 Jason Zhang, eagleboost@msn.com

  * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
  * EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
  * WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.

________________________________________________________________________________________*/
#endregion File Info Header

namespace Splash
{
  using System;
  using System.Threading;
  using System.Windows.Threading;
  using Microsoft.Practices.Composite.Events;
  using Microsoft.Practices.Composite.Modularity;
  using Microsoft.Practices.Unity;
  using Events;
  using Interfaces;
  using ViewModels;
  using Views;
  using Microsoft.Practices.Composite.Presentation.Events;

  public class Module : IModule
  {
    #region ctors
    public Module(IUnityContainer container, IEventAggregator eventAggregator, IShell shell)
    {
      Container = container;
      EventAggregator = eventAggregator;
      Shell = shell;
    }
    #endregion

    #region Private Properties
    private IUnityContainer Container { get; set; }

    private IEventAggregator EventAggregator { get; set; }

    private IShell Shell { get; set; }

    private AutoResetEvent WaitForCreation { get; set; }
    #endregion

    public void Initialize()
    {
      Dispatcher.CurrentDispatcher.BeginInvoke(
        (Action) (() =>
                    {
                      Shell.Show();
                      EventAggregator.GetEvent<CloseSplashEvent>().Publish(new CloseSplashEvent());
                    }));

      WaitForCreation = new AutoResetEvent(false);

      ThreadStart showSplash =
        () =>
          {
            Dispatcher.CurrentDispatcher.BeginInvoke(
              (Action) (() =>
                          {
                            Container.RegisterType<SplashViewModel, SplashViewModel>();
                            Container.RegisterType<SplashView, SplashView>();

                            var splash = Container.Resolve<SplashView>();
                            EventAggregator.GetEvent<CloseSplashEvent>().Subscribe(
                              e => splash.Dispatcher.BeginInvoke((Action) splash.Close),
                              ThreadOption.PublisherThread, true);

                            splash.Show();

                            WaitForCreation.Set();
                          }));

            Dispatcher.Run();
          };

      var thread = new Thread(showSplash) {Name = "Splash Thread", IsBackground = true};
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();

      WaitForCreation.WaitOne();
    }
  }
}
