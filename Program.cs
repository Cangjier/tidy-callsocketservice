using CallSocketService;
using TidyConsole;
using TidyHPC.Extensions;
using TidyHPC.LiteJson;
using TidyHPC.Loggers;
using TidyHPC.Routers;
using TidyHPC.Routers.Args;
using TidySocket.V2;

async Task call(
    [ArgsAliases("-n", "--name")] string interfaceName,
    [ArgsAliases("-s", "--server")] string serverName,
    [ArgsAliases("-as", "--auto-start-server")] bool isAutoStartServer,
    [ArgsAliases("-i", "--input")] string inputPath,
    [ArgsAliases("-o", "--output")] string outputPath,
    [ArgsAliases("-l", "--logger")] string? loggerPath,
    [ArgsAliases("-p", "--port")] string? port = null,
    [ArgsAliases("-ht", "--heartbeat-timeout")] string? heartbeatTimeout = null,
    [ArgsAliases("-it","--ini-timeout")]string? iniTimeout=null)
{
    if (loggerPath != null)
    {
        Logger.FilePath = loggerPath;
    }
    var input = Json.Load(inputPath);
    var output = Json.NewObject();
    var heartbeatTimespan = heartbeatTimeout == null ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(int.Parse(heartbeatTimeout));
    var iniTimespan = iniTimeout == null ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(int.Parse(iniTimeout));
    var env = new ClientApplication();
    var socketServers = await Util.GetRegisteredSocketServers(env, serverName);
    Logger.InfoParameter("Socket Servers", socketServers.Join(","));
    string? validPort = port;
    if (validPort == null)
    {
        validPort = await socketServers.FindAsync(async x =>
        {
            if (!int.TryParse(x, out int port))
            {
                Logger.Error($"Port is not a number:{x}");
                return false;
            }
            var result = await Util.HeartBeat(port, heartbeatTimespan);
            return result;
        });
    }
    if (validPort == null)
    {
        if (!isAutoStartServer)
        {
            throw new Exception("No valid port");
        }
        //没有可用的Cad服务
        //需要尝试启动Cad服务
        var serverPath = Util.TryEvalString(env.GetStringFromConfig([serverName, "Path"], ""));
        var serverArgs = Util.TryEvalString(env.GetStringFromConfig([serverName, "Arguments"], ""));
        var serverWorkingDirectory = Util.TryEvalString(env.GetStringFromConfig([serverName, "WorkingDirectory"], ""));
        var serverInstall = Util.TryEvalString(env.GetStringFromConfig([serverName, "Install"], ""));
        Logger.InfoParameters(new Json()
        {
            ["serverPath"] = serverPath,
            ["serverArgs"] = serverArgs,
            ["serverWorkingDirectory"] = serverWorkingDirectory,
            ["serverInstall"] = serverInstall
        });
        if (!File.Exists(serverPath))
        {
            throw new Exception($"ServerPath not found:`{serverPath}`");
        }
        if (serverInstall.Length > 0)
        {
            Logger.Info($"Install Server:{serverInstall}");
            Consoler.Theme theme = Consoler.Theme.NextTheme;
            await Consoles.ExecuteCommand(Path.GetDirectoryName(Environment.ProcessPath) ?? "", serverInstall, new Events(async line =>
            {
                Consoler.WriteLine(line.Content, theme);
                await Task.CompletedTask;
            }, async () =>
            {
                Consoler.WriteLine("Install Server Finished", theme);
                await Task.CompletedTask;
            }));
            Logger.Info($"Install Server Finished");
        }
        _ = Consoles.StartProgram(serverWorkingDirectory, serverPath, serverArgs, null);

        using CancellationTokenSource cts = new();
        _ = Task.Run(async () =>
        {
            await Task.Delay(iniTimespan);
            cts.Cancel();
        });
        while (true)
        {
            if (cts.IsCancellationRequested)
            {
                Logger.Error("Connect To CAD Server Timeout");
                throw new Exception("Timeout");
            }
            await Task.Delay(3000);
            var newSocketServers = await Util.GetRegisteredSocketServers(env, serverName);
            if (!newSocketServers.All(x => socketServers.Contains(x)))
            {
                validPort = await newSocketServers.FindAsync(async x =>
                {
                    if (!int.TryParse(x, out int port))
                    {
                        Logger.Error($"Port is not a number:{x}");
                        return false;
                    }
                    var result = await Util.HeartBeat(port, heartbeatTimespan);
                    return result;
                });
                if (validPort != null)
                {
                    break;
                }
                else
                {
                    socketServers = socketServers.Concat(newSocketServers).Distinct();
                }
            }
        }
    }
    if (validPort == null)
    {
        throw new Exception("No valid port");
    }
    env.Start(int.Parse(validPort));
    var result = await Util.RemoteEval(env, interfaceName, File.ReadAllText(inputPath, Util.UTF8), TimeSpan.FromMinutes(5));
    if (result.Target.IsNull)
    {
        throw new Exception("RemoteEval Result is null");
    }
    Console.WriteLine(result.ToString());
    File.WriteAllText(outputPath, result.data.ToString(), Util.UTF8);
}

ArgsRouter argsRouter = new();
argsRouter.Register(["call"], call);

await argsRouter.Route(args);
