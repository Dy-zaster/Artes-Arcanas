using Laa.Server.Networking;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var server = new ServerHost();
await server.RunAsync(cts.Token);
