using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private const string ServerUri = "ws://192.168.1.97:6001/battleserver_1/websocket";
    private const int NumberOfClients = 2000;
    private const int Offset = 0;
    public const double SendIntervalSeconds = 0.2;
    class ClientData
    {
        public ClientWebSocket Socket;
        public int ClientId;
        public long UID;
        public int MapId;
        public CancellationTokenSource ReceiveCTS;
        public CancellationTokenSource SendCTS;
        public double X;
        public double Y;
        public double Angle;
        public int MoveTime;
        public double SendTime;
        public double WaitTime;
        public ClientData(int id)
        {
            ClientId = id;
            ReceiveCTS = new CancellationTokenSource();
            SendCTS = new CancellationTokenSource();
            SendTime = 5;
            WaitTime = 0;
        }
        public void Send()
        {
            SendTime -= Program.SendIntervalSeconds;
            if (SendTime <= 0.0)
            {
                var r = new Random();
                WaitTime = r.NextDouble() * 3.0 + 2.0;
            }
        }
        public void Wait()
        {
            WaitTime -= Program.SendIntervalSeconds;
            if (WaitTime <= 0.0)
            {
                var r = new Random();
                SendTime = r.NextDouble() * 3.0 + 2.0;
            }
        }


    }
    static async Task Main(string[] args)
    {
        var tasks = new Task[NumberOfClients];

        for (int i = 0; i < NumberOfClients; i++)
        {
            int clientId = i + Offset;
            tasks[i] = Task.Run(async () => await StartClientAsync(new ClientData(clientId)));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task StartClientAsync(ClientData clientData)
    {
        var clientId = clientData.ClientId;
        using (var client = new ClientWebSocket())
        {
            clientData.Socket = client;
            try
            {
                await client.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
                Console.WriteLine($"Client {clientId} connected to server.");

                var receiveTask = Task.Run(async () => await ReceiveMessagesAsync(clientData));
                var sendTask = Task.Run(async () => await SendMessagesAfterResponseAsync(clientData));

                //await client.SendAsync(new ArraySegment<byte>(GetReqMsgCreateBattlePlayer(clientData)), WebSocketMessageType.Text, true, clientData.SendCTS.Token);
                await client.SendAsync(new ArraySegment<byte>(GetReqMsgLoginBattlePlayer(clientData)), WebSocketMessageType.Text, true, clientData.SendCTS.Token);

                await Task.WhenAll(sendTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientId} encountered an error: {ex.Message}");
            }
        }
    }
    private static async Task SendMessagesAfterResponseAsync(ClientData data)
    {
        var client = data.Socket;
        var clientId = data.ClientId;

        try
        {
            while (!data.SendCTS.Token.IsCancellationRequested && client.State == WebSocketState.Open)
            {

                if (data.UID == 0 || data.MapId == 0)
                {
                    await Task.Delay(100, data.SendCTS.Token);
                    continue;
                }
                if (data.WaitTime > 0)
                {
                    data.Wait();
                }
                else if (data.SendTime > 0)
                {
                    try
                    {
                        var messageBytes = GetReqMsgPlayerStateSync(data);
                        await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, data.SendCTS.Token);

                    }
                    finally
                    {
                    }
                    data.Send();
                }

                await Task.Delay((int)(SendIntervalSeconds * 1000), data.SendCTS.Token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client {clientId} failed to send message: {ex.Message}");
        }
        finally
        {
            data.SendCTS.Cancel();
        }
    }
    private static async Task ReceiveMessagesAsync(ClientData data)
    {
        var buffer = new byte[1024 * 10];
        var segment = new ArraySegment<byte>(buffer);
        var client = data.Socket;
        var clientId = data.ClientId;
        while (client.State == WebSocketState.Open)
        {
            try
            {
                var result = await client.ReceiveAsync(segment, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                //Console.WriteLine($"Client {clientId}  to receive message: {message}");
                var list = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(message);

                foreach (var item in list)
                {
                    if (item.Key == "MsgClientHeader")
                    {
                        continue;
                    }
                    if (item.Key == "ResMsgLoginBattlePlayer")
                    {
                        var login = item.Value.ToObject<ResMsgLoginBattlePlayer>();
                        data.UID = login.BattlePlayerData.A;
                        await client.SendAsync(new ArraySegment<byte>(GetReqMsgPlayerEnterRoom(1, data)), WebSocketMessageType.Text, true, data.ReceiveCTS.Token);
                        await Task.Delay(100, data.ReceiveCTS.Token);
                        data.MapId = 1;
                    }
                }

                //var json = JsonConvert.DeserializeObject<LoginData>(message);
                //data.UID = json.ResMsgCreateBattlePlayer.BattlePlayerData.PlayerInstId;

                //await client.SendAsync(new ArraySegment<byte>(GetReqMsgPlayerEnterRoom(1, data)), WebSocketMessageType.Text, true, data.ReceiveCTS.Token);

                //await Task.Delay(1000, data.ReceiveCTS.Token);
                //data.MapId = 1;
                await Task.Delay(-1);
                //break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientId} failed to receive message: {ex.Message}");
                break;
            }
        }
    }
    private static string CreateMessage(int clientId)
    {
        var message = new
        {
            ClientId = clientId,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Data = "Test data"
        };

        return JsonConvert.SerializeObject(message);
    }
    private static byte[] GetReqMsgCreateBattlePlayer(ClientData data)
    {
        var body = new
        {
            Account = "Bot_" + data.ClientId,
            Password = "Bot_" + data.ClientId,
            PlayerName = "Bot_" + data.ClientId,
        };
        return GetMessage("ReqMsgCreateBattlePlayer", body, data);
    }
    private static byte[] GetReqMsgLoginBattlePlayer(ClientData data)
    {
        var body = new
        {
            Account = "Bot_" + data.ClientId,
            Password = "Bot_" + data.ClientId,
        };
        return GetMessage("ReqMsgLoginBattlePlayer", body, data);
    }
    private static byte[] GetReqMsgPlayerEnterRoom(int mapId, ClientData data)
    {
        var body = new
        {
            MapCfgId = mapId
        };
        return GetMessage("ReqMsgPlayerEnterRoom", body, data);
    }
    private static byte[] GetReqMsgPlayerStateSync(ClientData data)
    {
        var r = new Random();
        int min = 200;
        int max = 3500;

        if (data.X < 1 || data.Y < 1)
        {
            data.X = r.Next(min, max);
            data.Y = r.Next(min, max);
            data.Angle = Math.PI * (double)(r.Next(0, 1000)) / 500;
        }
        else
        {
            var dirx = Math.Cos(data.Angle);
            var diry = Math.Sin(data.Angle);
            data.X += dirx * 30;
            data.Y += diry * 30;
            data.MoveTime++;
            if (data.MoveTime > 30)
            {
                data.Angle = Math.Atan2(r.Next(min, max) - data.Y, r.Next(min, max) - data.X);
                data.MoveTime = 0;
            }

        }


        BattlePlayerStateData state;
        state.B = new int[] { (int)data.X * 1, (int)data.Y * 1, 92 * 1, 0, 0, 0 };
        state.A = data.UID;
        state.C = "";

        ReqMsgPlayerStateSync sync;
        sync.BattlePlayerStateData = state;

        return GetMessage("ReqMsgPlayerStateSync", sync, data);
    }
    private static byte[] GetMessage(string name, object json, ClientData data)
    {
        var header = new { PlayerInstId = data.UID };
        var msg = new Dictionary<string, object>();
        msg["MsgServerHeader"] = header;
        msg[name] = json;
        var str = JsonConvert.SerializeObject(msg);
        //Console.WriteLine($"Client {data.ClientId} sent message: " + str);
        return Encoding.UTF8.GetBytes(str);
    }

    struct LoginData
    {
        public ResMsgCreateBattlePlayer ResMsgCreateBattlePlayer;
    }
    struct ResMsgCreateBattlePlayer
    {
        public BattlePlayerData BattlePlayerData;
    }
    struct BattlePlayerData
    {
        public long A;
    }
    struct BattlePlayerStateData
    {
        // 玩家实例Id
        public long A;
        // 坐标
        public int[] B;
        // 扩展json数据
        public string C;
    }
    struct ReqMsgPlayerStateSync
    {
        public BattlePlayerStateData BattlePlayerStateData;
    }

    struct ResMsgLoginBattlePlayer
    {
        public BattlePlayerData BattlePlayerData;
    }
}