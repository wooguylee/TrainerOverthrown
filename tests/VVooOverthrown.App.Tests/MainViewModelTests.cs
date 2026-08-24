using VVooOverthrown.App.Services;
using VVooOverthrown.App.ViewModels;
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
    public async Task ConnectEnablesVerifiedControlsOnlyForAllowedSession()
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
                SessionDecision = "Allowed",
                Capabilities = ["player.godMode", "world.timeScale"],
            },
        };
        var viewModel = new MainViewModel(service);

        await viewModel.ConnectHelperAsync();

        Assert.True(viewModel.CanUseTrainer);
        Assert.Equal("로컬 싱글플레이 확인됨", viewModel.SessionMessage);
    }

    private sealed class FakeApplicationService : ITrainerApplicationService
    {
        private readonly ApplicationSnapshot _snapshot;

        public FakeApplicationService(ApplicationSnapshot snapshot) => _snapshot = snapshot;

        public PipeResponse ConnectResponse { get; set; } = new();

        public Task<ApplicationSnapshot> GetSnapshotAsync(string gameRoot, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);

        public Task InstallAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public void LaunchGame(string gameRoot)
        {
        }

        public Task<PipeResponse> ConnectHelperAsync(string gameRoot, CancellationToken cancellationToken) =>
            Task.FromResult(ConnectResponse);

        public Task<PipeResponse> SendTrainerCommandAsync(PipeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(ConnectResponse);

        public Task ResetAndDisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
