using System.Threading;
using System.Windows.Threading;
using Xunit;

namespace VVooOverthrown.App.Tests;

public sealed class MainWindowSmokeTests
{
    [Fact]
    public void WindowCanCreateBindingsAndClose()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                window.Show();
                window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.Close();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The WPF smoke-test thread timed out.");
        Assert.Null(failure);
    }
}

