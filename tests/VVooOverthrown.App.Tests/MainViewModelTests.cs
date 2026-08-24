using VVooOverthrown.App.Services;
using VVooOverthrown.App.ViewModels;
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

    private sealed class FakeApplicationService : ITrainerApplicationService
    {
        private readonly ApplicationSnapshot _snapshot;

        public FakeApplicationService(ApplicationSnapshot snapshot) => _snapshot = snapshot;

        public Task<ApplicationSnapshot> GetSnapshotAsync(string gameRoot, CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);

        public Task InstallAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(string gameRoot, CancellationToken cancellationToken) => Task.CompletedTask;

        public void LaunchGame(string gameRoot)
        {
        }
    }
}

