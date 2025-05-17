using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

public class ProtoManager
{
    //类型到协议号
    protected Dictionary<Type, int> typeProtocol = new Dictionary<Type, int>();
    //协议号到解析
    protected Dictionary<int, Func<byte[], int, int, IMessage>> protocolParser = new Dictionary<int, Func<byte[], int, int, IMessage>>();
    //协议号到处理器
    protected Dictionary<int, Action<IMessage>> protocolProcessor = new Dictionary<int, Action<IMessage>>();

    public void AddMapping<T>(Func<byte[], int, int, IMessage> parser = null, Action<IMessage> processor = null)
    {
        AddMapping(typeof(T), parser, processor);
    }
    public void AddMapping(Type type, Func<byte[], int, int, IMessage> parser = null, Action<IMessage> processor = null)
    {
        //替换稳定方法
        var protocol = StringToHashCode(type.FullName);
        if (!typeProtocol.ContainsKey(type))
        {
            typeProtocol.Add(type, protocol);
        }
        if (parser != null)
        {
            if (!protocolParser.ContainsKey(protocol))
            {
                protocolParser.Add(protocol, parser);
            }
            else
            {
                Debug.Instance.Log(type.FullName + " exist");
            }

        }
        if (processor != null)
        {
            protocolProcessor.Add(protocol, processor);
        }
    }
    public int StringToHashCode(string input)
    {
        unchecked // 允许溢出，这通常用于哈希函数以提高分布性  
        {
            int hash = 17; // 初始哈希值（可以是任何非零整数）  
            foreach (char c in input)
            {
                hash = hash * 23 + c; // 使用一个质数乘法器和加法器  
            }
            return hash;
        }
    }
    public int GetProtocol<T>()
    {
        return GetProtocol(typeof(T));
    }
    public int GetProtocol(Type type)
    {
        if (typeProtocol.TryGetValue(type, out var protocol))
        {
            return protocol;
        }
        {
            return StringToHashCode(type.FullName);
        }
    }
    public byte[] SendMessages(List<IMessage> messages)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            var len = messages.Count;
            for (var i = 0; i < len; i++)
            {
                var msg = messages[i];
                var protocol = GetProtocol(msg.GetType());
                if (protocol == 0)
                {
                    continue;
                }
                var first = stream.Position;
                stream.Seek(first + 8, SeekOrigin.Begin);
                msg.WriteTo(stream);
                var last = stream.Position;
                var size = last - first;
                stream.Seek(first, SeekOrigin.Begin);
                //写长度
                stream.Write(BitConverter.GetBytes((int)size));

                //协议号
                stream.Write(BitConverter.GetBytes(protocol));
                stream.Seek(last, SeekOrigin.Begin);
            }
            return stream.ToArray();
        }
    }
    public List<IMessage> FromBytes(byte[] bytes)
    {
        var list = new List<IMessage>();
        if (bytes == null)
        {
            return list;
        }
        int current = 0;
        int max = 999;
        while (true && max-- > 0)
        {
            if (current + 8 > bytes.Length)
            {
                break;
            }
            var size = BitConverter.ToInt32(bytes, current);
            var protocol = BitConverter.ToInt32(bytes, current + 4);
            if (protocolParser.TryGetValue(protocol, out var func))
            {
                try
                {
                    var msg = func.Invoke(bytes, current + 8, size - 8);
                    list.Add(msg);
                    if (protocolProcessor.TryGetValue(protocol, out var processor))
                    {
                        processor.Invoke(msg);
                    }
                }
                catch (System.Exception)
                {

                }


            }
            current += size;
        }
        return list;
    }
}
