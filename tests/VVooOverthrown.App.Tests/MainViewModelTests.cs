using VVooOverthrown.App.Services;
using VVooOverthrown.App.ViewModels;
using VVooOverthrown.Helper.Features;
using VVooOverthrown.Helper.Transport;
using Xunit;

namespace VVooOverthrown.App.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshEnablesInstallForSupportedUninstalledBuild()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown",
            pathValid: true,
            buildSupported: true,
            installed: false,
            gameRunning: false,
            helperConnected: false));
        var viewModel = new MainViewModel(service);

        await viewModel.RefreshAsync();

        Assert.True(viewModel.CanInstall);
        Assert.False(viewModel.CanRemove);
        Assert.Equal("한글 패치 설치 가능", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConnectEnablesTestModeControlsWhenSessionIsUncertain()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown",
            pathValid: true,
            buildSupported: true,
            installed: true,
            gameRunning: true,
            helperConnected: false))
        {
            ConnectResponse = new PipeResponse
            {
                Ok = true,
                TestModeEnabled = true,
                SessionDecision = "Uncertain",
                Capabilities = ["player.godMode", "inventory.resource", "kingdom.resource"],
            },
        };
        var viewModel = new MainViewModel(service);

        await viewModel.ConnectHelperAsync();

        Assert.True(viewModel.CanUseTrainer);
        Assert.Equal("테스트 모드 · 세션 판정: 미확인", viewModel.SessionMessage);
    }

    [Fact]
    public async Task InventorySetSendsSelectedResourceAndAmount()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown",
            pathValid: true,
            buildSupported: true,
            installed: true,
            gameRunning: true,
            helperConnected: true))
        {
            ConnectResponse = new PipeResponse
            {
                Ok = true,
                TestModeEnabled = true,
                SessionDecision = "Uncertain",
            },
        };
        var viewModel = new MainViewModel(service)
        {
            SelectedInventoryResource = TrainerResourceOptions.All.Single(option => option.Value == 18),
            InventoryAmountInput = "250",
        };

        await viewModel.SetInventoryResourceAsync();

        Assert.NotNull(service.LastRequest);
        Assert.Equal(TrainerCommands.InventorySet, service.LastRequest!.Command);
        Assert.Equal(18, service.LastRequest.ResourceType);
        Assert.Equal(250, service.LastRequest.Amount);
    }

    [Theory]
    [InlineData("abc", false)]
    [InlineData("2147483648", false)]
    [InlineData("-1", false)]
    [InlineData("0", true)]
    [InlineData("1000000000", true)]
    public void InventoryMutationRequiresValidCurrentText(string input, bool expected)
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown", true, true, true, true, true));
        var viewModel = new MainViewModel(service);

        viewModel.InventoryAmountInput = input;

        Assert.Equal(expected, viewModel.IsInventoryAmountValid);
    }

    [Fact]
    public async Task InventoryAddPreservesEnteredDeltaAndDisplaysObservedTotal()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown", true, true, true, true, true))
        {
            ConnectResponse = new PipeResponse
            {
                Ok = true,
                TestModeEnabled = true,
                SessionDecision = "Uncertain",
                SelectedResourceType = 1,
                InventoryAmount = 150,
            },
        };
        var viewModel = new MainViewModel(service)
        {
            InventoryAmountInput = "100",
        };

        await viewModel.AddInventoryResourceAsync();

        Assert.Equal("100", viewModel.InventoryAmountInput);
        Assert.Contains("150", viewModel.InventoryResultMessage);
        Assert.Equal(100, service.LastRequest!.Amount);
    }

    [Fact]
    public async Task InvalidInventoryInputDoesNotSendRequest()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown", true, true, true, true, true));
        var viewModel = new MainViewModel(service)
        {
            InventoryAmountInput = "not-a-number",
        };

        await viewModel.SetInventoryResourceAsync();

        Assert.Null(service.LastRequest);
        Assert.Contains("입력 오류", viewModel.InventoryInputMessage);
    }

    [Fact]
    public async Task InfiniteCtrlMovementSendsToggleAndUsesHelperResponse()
    {
        var service = new FakeApplicationService(new ApplicationSnapshot(
            @"W:\Games\Overthrown",
            pathValid: true,
            buildSupported: true,
            installed: true,
            gameRunning: true,
            helperConnected: true))
        {
            ConnectResponse = new PipeResponse
            {
                Ok = true,
                TestModeEnabled = true,
                InfiniteCtrlMovementEnabled = true,
            },
        };
        var viewModel = new MainViewModel(service);

        await viewModel.SetInfiniteCtrlMovementAsync(true);

        Assert.NotNull(service.LastRequest);
        Assert.Equal(TrainerCommands.InfiniteCtrlMovement, service.LastRequest!.Command);
        Assert.True(service.LastRequest.Enabled);
        Assert.True(viewModel.InfiniteCtrlMovementEnabled);
    }

    private sealed class FakeApplicationService : ITrainerApplicationService
    {
        private readonly ApplicationSnapshot _snapshot;

        public FakeApplicationService(ApplicationSnapshot snapshot) => _snapshot = snapshot;

        public PipeResponse ConnectResponse { get; set; } = new();

        public PipeRequest? LastRequest { get; private set; }

        public Task<ApplicationSnapshot> GetSnapshotAsync(string gameRoot, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);

        public Task InstallAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public void LaunchGame(string gameRoot)
        {
        }

        public Task<PipeResponse> ConnectHelperAsync(string gameRoot, CancellationToken cancellationToken) =>
            Task.FromResult(ConnectResponse);

        public Task<PipeResponse> SendTrainerCommandAsync(PipeRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(ConnectResponse);
        }

        public Task ResetAndDisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
