using VVooOverthrown.Core.State;
using Xunit;

namespace VVooOverthrown.App.Tests;

public sealed class TrainerMainStateTests
{
    [Theory]
    [InlineData(false, false, "게임 경로 확인 필요")]
    [InlineData(true, false, "지원하지 않는 게임 빌드")]
    [InlineData(true, true, "한글 패치 설치 가능")]
    public void StatusMessageMatchesBuildState(bool pathValid, bool buildSupported, string expected)
    {
        Assert.Equal(
            expected,
            TrainerMainState.Evaluate(pathValid, buildSupported, installed: false, gameRunning: false, helperConnected: false).Message);
    }

    [Fact]
    public void InstalledRunningGameRequestsConnection()
    {
        var state = TrainerMainState.Evaluate(
            pathValid: true,
            buildSupported: true,
            installed: true,
            gameRunning: true,
            helperConnected: false);

        Assert.Equal(TrainerStage.HelperConnectionRequired, state.Stage);
        Assert.Equal("게임 연결 필요", state.Message);
    }
}

