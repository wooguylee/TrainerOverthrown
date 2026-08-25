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

    private async void Heal_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.HealAsync();

    private async void ApplyStaminaFactor_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyStaminaFactorAsync();

    private async void InfiniteCtrlMovementOn_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetInfiniteCtrlMovementAsync(true);

    private async void InfiniteCtrlMovementOff_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetInfiniteCtrlMovementAsync(false);

    private async void ApplyMovementSpeed_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyMovementSpeedAsync();

    private async void ApplyTimeScale_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyTimeScaleAsync();

    private async void ApplyRegularJumpMultiplier_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyRegularJumpMultiplierAsync();

    private async void ApplySpecialMovementMultiplier_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplySpecialMovementMultiplierAsync();

    private async void ApplyGravityMultiplier_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ApplyGravityMultiplierAsync();

    private async void InventoryQuery_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.QueryInventoryResourceAsync();

    private async void InventorySet_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetInventoryResourceAsync();

    private async void InventoryAdd_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.AddInventoryResourceAsync();

    private async void InventorySubtract_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SubtractInventoryResourceAsync();

    private async void KingdomQuery_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.QueryKingdomResourceAsync();

    private async void KingdomSet_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetKingdomResourceAsync();

    private async void KingdomAdd_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.AddKingdomResourceAsync();

    private async void KingdomSubtract_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SubtractKingdomResourceAsync();

    private async void ResetTrainer_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ResetTrainerAsync();
}
