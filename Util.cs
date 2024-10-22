using System.Security.Cryptography;
using System.Text;
using TidyHPC.LiteJson;
using TidyHPC.Loggers;
using TidySocket.V2;

namespace CallSocketService;
internal class Util
{
    public static Encoding UTF8 { get; } = new UTF8Encoding(false);

    public static ClientApplication NewClientApplication() => new();

    public static async Task<bool> HeartBeat(int port, TimeSpan timeout)
    {
        Logger.Info($"[port {port}] Heartbeat start {timeout}");
        TaskCompletionSource<bool> taskCompletionSource = new();
        TaskCompletionSource waitConnect = new();
        _ = Task.Run(async () =>
        {
            await Task.Delay(timeout);
            Logger.Info($"[port {port}] Heartbeat timeout {timeout}");
            waitConnect.TrySetResult();
            taskCompletionSource.TrySetResult(false);
        });
        var tempEnv = NewClientApplication();
        tempEnv.Start(port, async () =>
        {
            Logger.Info($"[port {port}] Heartbeat connected {timeout}");
            waitConnect.TrySetResult();
            await Task.CompletedTask;
        });
        await waitConnect.Task;
        tempEnv.RemoteEval("HeartBeat", "", msg =>
        {
            Logger.Info($"[port {port}] Heartbeat response {msg}");
            taskCompletionSource.TrySetResult(msg.success || msg.code == "200");
        });
        var result = await taskCompletionSource.Task;
        tempEnv.Close();
        return result;
    }

    public static async Task<NetMessageInterface> RemoteEval(ClientApplication env, string name, string args, TimeSpan timeout)
    {
        NetMessageInterface result = Json.Null;
        await env.RemoteEvalOnce(name, args, timeout, async msg =>
        {
            result = Json.Parse(msg.ToString());
            await Task.CompletedTask;
        }, async () =>
        {
            Logger.Info($"Timeout RemoteEval {name} {args}");
            await Task.CompletedTask;
        });
        return result;
    }

    public static async Task<Json> RemoteEval2(ClientApplication env, string name, string args, TimeSpan timeout)
    {
        TaskCompletionSource<Json> taskCompletionSource = new();
        CancellationTokenSource cancellationTokenSource = new(timeout);
        cancellationTokenSource.Token.Register(() =>
        {
            taskCompletionSource.TrySetResult(Json.Null);
        });
        env.RemoteEval(name, args, msg =>
        {
            taskCompletionSource.SetResult(Json.Parse(msg.ToString()));
        });
        var result = await taskCompletionSource.Task;
        cancellationTokenSource.Dispose();
        return result;
    }

    public static async Task<IEnumerable<string>> GetRegisteredSocketServers(ClientApplication env,string serverName)
    {
        var socketServersResult = (await env.ApiServer.ApiByName("GetRegisteredSocketServers", new Dictionary<string, string>()
            {
                {"ServerName",serverName }
            })).GetByPath(["Response", "Body", "data"]);
        if (socketServersResult.IsNull)
        {
            return [];
        }
        return socketServersResult.AsString.Split(',').Where(x => x.Length != 0).Reverse();
    }

    public static async Task UnregisteredSocketServer(ClientApplication env, string serverName,int port)
    {
        await env.ApiServer.ApiByName("UnregisterSocketServer", new Dictionary<string, string>()
            {
                {"ServerName",serverName },
                {"Port",port.ToString() }
            }
        );
    }

    public static Json EvalString(string script)
    {
        return Cangjie.TypeSharp.TSScriptEngine.Run(script, stepContext =>
        {

        }, runtimeContext =>
        {

        });
    }

    public static string TryEvalString(string script)
    {
        if(script.StartsWith("$"))
        {
            return EvalString(script[1..]).ToString();
        }
        return script;
    }

    public static string ComputeSha256Hash(string rawData)
    {
        // 创建一个SHA256对象  
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // 将输入字符串转换为字节数组  
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // 将字节数组转换为十六进制字符串  
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public static int ComputeHash(string input)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256Hash.ComputeHash(bytes);

            // 取哈希值的前4个字节转换为一个整数  
            int intHash = BitConverter.ToInt32(hash, 0);
            return intHash;
        }
    }

    public static string ComputeMD5Hash(string rawData)
    {
        // 创建一个MD5对象  
        using MD5 md5Hash = MD5.Create();
        // 将输入字符串转换为字节数组并计算哈希数据  
        byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

        // 创建一个 Stringbuilder 来收集字节并创建字符串  
        StringBuilder builder = new StringBuilder();

        // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串  
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }

        // 返回十六进制字符串  
        return builder.ToString();
    }

    public static string ComputeMD5Hash(byte[] rawData)
    {
        // 创建一个MD5对象  
        using MD5 md5Hash = MD5.Create();
        // 将输入字符串转换为字节数组并计算哈希数据  
        byte[] bytes = md5Hash.ComputeHash(rawData);

        // 创建一个 Stringbuilder 来收集字节并创建字符串  
        StringBuilder builder = new StringBuilder();

        // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串  
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }

        // 返回十六进制字符串  
        return builder.ToString();
    }

    public static string ComputeMD5Hash(FileStream stream)
    {
        // 创建一个MD5对象  
        using (MD5 md5Hash = MD5.Create())
        {
            // 将输入字符串转换为字节数组并计算哈希数据  
            byte[] bytes = md5Hash.ComputeHash(stream);

            // 创建一个 Stringbuilder 来收集字节并创建字符串  
            StringBuilder builder = new StringBuilder();

            // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串  
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            // 返回十六进制字符串  
            return builder.ToString();
        }
    }

    public static string ComputeBase64(string rawData)
    {
        return Convert.ToBase64String(UTF8.GetBytes(rawData));
    }

    public static string ComputeBase64(byte[] rawData)
    {
        return Convert.ToBase64String(rawData);
    }
}
