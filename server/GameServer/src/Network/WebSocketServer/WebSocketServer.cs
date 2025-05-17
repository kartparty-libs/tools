
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;

public class WebSocketServer
{
    private int m_nBlocking = 1;
    public void Initializer()
    {
    }

    public async Task RunServerAsync(int i_nPort)
    {
        Debug.Instance.LogInfo($"WebSocketServer Initializer Start Port -> {i_nPort}");
        // 用于接受连接，通常只需要一个线程
        IEventLoopGroup group = new MultithreadEventLoopGroup(1);
        // 用于处理接受的连接，默认使用 CPU 核心数 * 2 的线程数
        IEventLoopGroup workGroup = new MultithreadEventLoopGroup();
        try
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(group, workGroup);
            bootstrap.Channel<TcpServerSocketChannel>();
            bootstrap
                .Option(ChannelOption.SoBacklog, 8192)
                .ChildOption(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new HttpServerCodec());
                        pipeline.AddLast(new HttpObjectAggregator(65536));
                        pipeline.AddLast(new WebSocketServerHandler());
                    }));

            IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.Any, i_nPort);

            Debug.Instance.LogInfo($"WebSocketServer Initializer Listening on {bootstrapChannel.LocalAddress} Succeed ");

            while (m_nBlocking > 0) { Thread.Sleep(1000); };

            await bootstrapChannel.CloseAsync();
        }
        finally
        {
            //关闭释放并退出
            await Task.WhenAll(
            group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
            workGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        }
    }
    public void Close()
    {
        System.Threading.Interlocked.Decrement(ref m_nBlocking);
    }
}