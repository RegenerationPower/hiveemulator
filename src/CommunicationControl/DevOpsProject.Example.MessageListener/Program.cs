using StackExchange.Redis;

class Program
{
    /// <summary>
    /// Sample app with only purpose to listen message bus and keep queue alive inside redis
    /// </summary>
    /// <returns></returns>
    static async Task Main()
    {
        Console.WriteLine("Connecting to Redis...");

        var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        var subscriber = redis.GetSubscriber();

        string channelName = "HiveChannel";

        await subscriber.SubscribeAsync(channelName, (channel, message) =>
        {
            Console.WriteLine($"Received message: {message}");
        });

        Console.WriteLine($"Listening for messages on channel: {channelName}");
        Console.WriteLine("Press any key to exit...");

        Console.ReadKey();
    }
}