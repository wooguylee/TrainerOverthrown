using System.IO;
using System.Windows;
using VVooOverthrown.App.Services;
using VVooOverthrown.Core.Discovery;

namespace VVooOverthrown.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var command = e.Args.FirstOrDefault(arg => arg is "--install" or "--remove");
        if (command is null)
        {
            new MainWindow().Show();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var gameRoot = GetArgumentValue(e.Args, "--game") ?? GameLocator.DefaultGameRoot;
        var resultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VVooOverthrown",
            "last-command.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        try
        {
            var service = new TrainerApplicationService();
            if (command == "--install")
            {
                await service.InstallAsync(gameRoot, CancellationToken.None);
            }
            else
            {
                await service.RemoveAsync(gameRoot, CancellationToken.None);
            }

            await File.WriteAllTextAsync(resultPath, $"SUCCESS {command} {DateTimeOffset.UtcNow:O}");
            Shutdown(0);
        }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(
                resultPath,
                $"FAILED {command} {exception.GetType().Name}: {exception.Message}");
            Shutdown(1);
        }
    }

    private static string? GetArgumentValue(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
