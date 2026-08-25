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

    private async void StaminaZero_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(0f);

    private async void StaminaQuarter_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(0.25f);

    private async void StaminaHalf_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(0.5f);

    private async void StaminaNormal_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(1f);

    private async void StaminaDouble_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(2f);

    private async void StaminaTen_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(10f);

    private async void StaminaHundred_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetStaminaFactorAsync(100f);

    private async void InfiniteCtrlMovementOn_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetInfiniteCtrlMovementAsync(true);

    private async void InfiniteCtrlMovementOff_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetInfiniteCtrlMovementAsync(false);

    private async void MovementTenth_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(0.1f);

    private async void MovementHalf_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(0.5f);

    private async void MovementNormal_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(1f);

    private async void MovementDouble_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(2f);

    private async void MovementQuad_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(4f);

    private async void MovementEight_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(8f);

    private async void MovementTwenty_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetMovementSpeedAsync(20f);

    private async void TimePause_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(0f);

    private async void TimeHalf_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(0.5f);

    private async void TimeNormal_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(1f);

    private async void TimeDouble_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(2f);

    private async void TimeQuad_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(4f);

    private async void TimeTen_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SetTimeScaleAsync(10f);

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
