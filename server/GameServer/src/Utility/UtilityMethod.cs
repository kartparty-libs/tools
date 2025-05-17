using Google.Protobuf;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Proto;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class UtilityMethod
{
    /// <summary>
    /// 获取当前Unix时间戳（毫秒）
    /// </summary>
    /// <returns></returns>
    public static long GetUnixTimeMilliseconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 获取当前北京零点时间戳
    /// </summary>
    /// <returns></returns>
    public static long GetZeroUnixTimeMillisecondsByBeiJing()
    {
        DateTimeOffset beijingNow = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8));
        DateTimeOffset midnightBeijing = new DateTimeOffset(beijingNow.Year, beijingNow.Month, beijingNow.Day, 0, 0, 0, TimeSpan.FromHours(8));
        return midnightBeijing.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 获取当前Unix零点时间戳
    /// </summary>
    /// <returns></returns>
    public static long GetZeroUnixTimeMilliseconds()
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateTimeOffset midnightUtc = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero);
        return midnightUtc.ToUnixTimeMilliseconds();
    }

    /// <summary>  
    /// 将日期时间字符串转换为UTC时间戳（毫秒）  
    /// </summary>  
    /// <param name="dateTimeString">日期时间字符串</param>  
    /// <param name="format">日期时间字符串的格式</param>  
    /// <returns>UTC时间戳（毫秒）</returns>  
    public static long ConvertToUtcTimestampMilliseconds(string dateTimeString, string format = "yyyyMMddHHmmss", int offsetHours = 0)
    {
        try
        {
            if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                TimeSpan timeSpan = dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return (long)timeSpan.TotalMilliseconds + offsetHours * 3600000;
            }
            return 0;
        }
        catch (Exception e)
        {
            Debug.Instance.LogError(e.Message);
            return 0;
        }
    }

    /// <summary>
    /// json序列化
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string JsonSerializeObject(object? value, Formatting formatting = Formatting.None)
    {
        try
        {
            return JsonConvert.SerializeObject(value, formatting);
        }
        catch (Exception e)
        {
            Debug.Instance.LogError(e.Message);
            return default;
        }
    }

    /// <summary>
    /// json反序列化
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T0? JsonDeserializeObject<T0>(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<T0>(value);
        }
        catch (Exception e)
        {
            Debug.Instance.LogError(e.Message);
            return default;
        }
    }

    /// <summary>
    /// JObject转类型
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T0? ObjecTo<T0>(JObject value)
    {
        try
        {
            return value.ToObject<T0>();
        }
        catch (Exception e)
        {
            Debug.Instance.LogError(e.Message);
            return default;
        }
    }

    /// <summary>
    /// 不允许以下字符：空格、特殊字符（非字母数字）、控制字符等 
    /// </summary>
    public static readonly Regex InvalidCharactersRegex = new Regex("[^a-zA-Z0-9_-]");

    /// <summary>
    /// 验证名字规范性
    /// </summary>
    /// <param name="i_sString"></param>
    /// <returns></returns>
    public static bool VerifyConventionByName(string i_sString)
    {
        if (i_sString.Length == 0)
        {
            return false;
        }

        return !InvalidCharactersRegex.IsMatch(i_sString);
    }

    /// <summary>
    /// 验证账号规范性
    /// </summary>
    /// <param name="i_sString"></param>
    /// <returns></returns>
    public static bool VerifyConventionByAccount(string i_sString)
    {
        if (i_sString.Length == 0)
        {
            return false;
        }

        return !InvalidCharactersRegex.IsMatch(i_sString);
    }

    /// <summary>  
    /// 验证邮箱规范性  
    /// </summary>  
    /// <param name="i_sEmail"></param>  
    /// <returns></returns>  
    public static bool VerifyConventionByEmail(string i_sEmail)
    {
        // 简单的邮箱正则表达式，适用于大多数常见情况  
        // 注意：这个正则表达式可能不是完美的，因为它不会检查域名的有效性（如顶级域是否存在）  
        string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        Regex emailRegex = new Regex(emailPattern);

        if (string.IsNullOrWhiteSpace(i_sEmail))
        {
            return false;
        }

        return emailRegex.IsMatch(i_sEmail);
    }

    /// <summary>
    /// 验证Telegram登录码
    /// </summary>
    /// <param name="initData"></param>
    /// <param name="botToken"></param>
    /// <returns></returns>
    public static bool VerifyTelegramLogin(string initData, string botToken)
    {
        // 解析查询字符串
        var queryParams = QueryHelpers.ParseQuery(initData);
        // 提取并保存 hash 参数，然后从数据中移除
        if (!queryParams.TryGetValue("hash", out var receivedHash)) { receivedHash = default; }
        queryParams.Remove("hash");
        // 1. 排除 hash 参数，生成 `{key}={value}` 格式的字符串数组
        var dataCheckString = string.Join("\n", queryParams.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        // 2. 计算 HMAC-SHA256 的密钥
        using (var hmac1 = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData")))
        {
            var secretKey = hmac1.ComputeHash(Encoding.UTF8.GetBytes(botToken));
            // 3. 使用生成的密钥计算最终的哈希值
            using (var hmac2 = new HMACSHA256(secretKey))
            {
                var calculatedHash = hmac2.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
                var calculatedHashHex = BitConverter.ToString(calculatedHash).Replace("-", "").ToLowerInvariant();
                // 4. 比较计算得到的哈希值与收到的 hash 参数
                return calculatedHashHex == receivedHash.ToString()?.ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// 字符串字符ASCII码相加
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static int SumAsciiValues(string input)
    {
        int sum = 0;
        foreach (char c in input)
        {
            sum += c;
        }
        return sum;
    }

    /// <summary>
    /// Json转消息体列表
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static List<IMessage> JsonToMessage(byte[] content)
    {
        List<IMessage> messages = new List<IMessage>();
        string requestBodyString = Encoding.UTF8.GetString(content);
        Dictionary<string, JObject> requestBody = UtilityMethod.JsonDeserializeObject<Dictionary<string, JObject>>(requestBodyString);
        if (requestBody != null)
        {
            foreach (var item in requestBody)
            {
                Type type = Type.GetType($"Proto.{item.Key}");
                if (type != default)
                {
                    var message = item.Value.ToObject(type);
                    if (message != null && message is IMessage __message)
                    {
                        if (message is MsgServerHeader || message is MsgClientHeader)
                        {
                            messages.Insert(0, __message);
                        }
                        else
                        {
                            messages.Add(__message);
                        }
                    }
                }
            }
        }
        return messages;
    }
}