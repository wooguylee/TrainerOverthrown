using System.Windows;
using VVooOverthrown.App.Services;
using VVooOverthrown.App.ViewModels;

namespace VVooOverthrown.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new TrainerApplicationService());
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.RefreshAsync();
        Closed += async (_, _) => await _viewModel.ResetAndDisconnectAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await _viewModel.RefreshAsync();

    private async void Install_Click(object sender, RoutedEventArgs e) => await _viewModel.InstallAsync();

    private async void Remove_Click(object sender, RoutedEventArgs e) => await _viewModel.RemoveAsync();

    private void Launch_Click(object sender, RoutedEventArgs e) => _viewModel.LaunchGame();

    private async void ConnectHelper_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ConnectHelperAsync();

    private async void GodModeOn_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetGodModeAsync(true);

    private async void GodModeOff_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetGodModeAsync(false);

    private async void TimeHalf_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(0.5f);

    private async void TimeNormal_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(1f);

    private async void TimeDouble_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(2f);
}
