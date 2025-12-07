using ArtesArcanas.Server.Core.Runtime;

var builder = ServerBuilder.CreateDefault();

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--root":
        case "-r":
            if (i + 1 < args.Length)
            {
                builder.UseRootPath(args[++i]);
            }
            break;
        case "--legacy-root":
        case "-d":
            if (i + 1 < args.Length)
            {
                builder.UseLegacyDataPath(args[++i]);
            }
            break;
        case "--options":
        case "-o":
            if (i + 1 < args.Length)
            {
                builder.UseOptionsFile(args[++i]);
            }
            break;
    }
}

await using var host = builder.Build();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

await host.StartAsync(cts.Token);
Console.WriteLine("Servidor inicializado. Presiona Ctrl+C para finalizar.");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // ignored
}

await host.StopAsync(CancellationToken.None);
