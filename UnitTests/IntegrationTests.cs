using System.Diagnostics;
using hzcache;
using StackExchange.Redis;

namespace UnitTests
{
    public class Mocko
    {
        public Mocko(long num)
        {
            this.num = num;
            str = num.ToString();
        }

        public long num { get; }
        public string str { get; }
    }

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestRedisBackplaneInvalidation()
        {
            var c1 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c1"});
            await Task.Delay(200);
            var c2 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c2"});

            Console.WriteLine("Adding 1 to c1");
            c1.Set("1", new Mocko(1));
            await Task.Delay(100);
            Console.WriteLine("Adding 1 to c2");
            c2.Set("1", new Mocko(2));
            await Task.Delay(100);

            Assert.IsNotNull(c1.Get<Mocko>("1"));
            Assert.IsNotNull(c2.Get<Mocko>("1"));
        }


        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestRedisClear()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var c1 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c1"});
            await Task.Delay(200);
            var c2 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c2"});

            Console.WriteLine("Adding 1 to c1");
            c1.Set("1", new Mocko(1));
            await Task.Delay(100);
            Console.WriteLine("Adding 2 to c2");
            c1.Set("2", new Mocko(2));
            Console.WriteLine("Adding 2 to c2");
            c2.Set("3", new Mocko(3));
            await Task.Delay(100);

            c1.Clear();
            await Task.Delay(2000);
            Assert.IsNull(c1.Get<Mocko>("1"));
            Assert.IsNull(c2.Get<Mocko>("2"));
            Assert.IsNull(c2.Get<Mocko>("3"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestRedisGet()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var c1 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c1"});
            await Task.Delay(200);
            var c2 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c2"});

            Console.WriteLine("Adding 1 to c1");
            c1.Set("1", new Mocko(10));
            await Task.Delay(100);
            Console.WriteLine("Getting 1 from c2");
            Assert.IsNotNull(c2.Get<Mocko>("1"));
        }


        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestRedisBackplaneDelete()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var c1 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c1"});
            await Task.Delay(200);
            var c2 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c2"});

            Console.WriteLine("Adding 1 to c1");
            c1.Set("1", new Mocko(1));
            await Task.Delay(100);
            Console.WriteLine("Adding 2 to c2");
            c1.Set("2", new Mocko(2));
            await Task.Delay(100);
            Console.WriteLine("Delete 1 from c2");
            c2.Remove("1");
            await Task.Delay(300);
            Assert.IsNull(c1.Get<Mocko>("1"));
            Assert.IsNotNull(c2.Get<Mocko>("2"));
        }


        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestRedisBackplaneDeleteByPattern()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var c1 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c1"});
            await Task.Delay(200);
            var c2 = new RedisBackplaneHzCache(
                new RedisBackplanceMemoryMemoryCacheOptions {redisConnectionString = "localhost", applicationCachePrefix = "test", instanceId = "c2"});

            c1.Set("11", new Mocko(11));
            c1.Set("12", new Mocko(12));
            c1.Set("22", new Mocko(22));
            c1.Set("13", new Mocko(13));
            c1.Set("23", new Mocko(23));
            c1.Set("33", new Mocko(33));
            Console.WriteLine("Deleting by pattern 2* on c2");
            await Task.Delay(200);
            c2.RemoveByPattern("2*");
            await Task.Delay(200);
            Assert.IsNotNull(c1.Get<Mocko>("11"));
            Assert.IsNotNull(c2.Get<Mocko>("12"));
            Assert.IsNotNull(c1.Get<Mocko>("13"));
            Assert.IsNotNull(c2.Get<Mocko>("33"));
            Assert.IsNull(c1.Get<Mocko>("22"));
            Assert.IsNull(c2.Get<Mocko>("23"));
            Console.WriteLine("Deleting by pattern 1*");
            c2.RemoveByPattern("1*");
            await Task.Delay(400);
            Assert.IsNull(c1.Get<Mocko>("11"));
            Assert.IsNull(c1.Get<Mocko>("12"));
            Assert.IsNull(c1.Get<Mocko>("13"));
            Assert.IsNotNull(c1.Get<Mocko>("33"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task TestDistributedInvalidationPerformance()
        {
            var iterations = 100000.0d;
            var redis = ConnectionMultiplexer.Connect("localhost");
            var c1 = new RedisBackplaneHzCache(new RedisBackplanceMemoryMemoryCacheOptions
            {
                redisConnectionString = "localhost",
                applicationCachePrefix = "test",
                defaultTTL = TimeSpan.FromSeconds(Math.Max(iterations / 10000, 20)),
                notificationType = NotificationType.Async
            });

            var c2 = new RedisBackplaneHzCache(new RedisBackplanceMemoryMemoryCacheOptions
            {
                redisConnectionString = "localhost:6379",
                applicationCachePrefix = "test",
                defaultTTL = TimeSpan.FromSeconds(Math.Max(iterations / 10000, 20)),
                notificationType = NotificationType.Async
            });

            Console.WriteLine("Adding 1 to c1");
            c1.Set("1", new Mocko(1));
            await Task.Delay(10);
            Console.WriteLine("Adding 1 to c2");
            c2.Set("1", new Mocko(1));
            await Task.Delay(20);
            Assert.IsNotNull(c1.Get<Mocko>("1"));
            Assert.IsNotNull(c2.Get<Mocko>("1"));

            var start = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                c1.Set("test" + i, new Mocko(i));
            }

            var end = start.ElapsedTicks;
            start.Restart();
            var size = 0;
            var max = Math.Max((int)iterations / 800, 20);
            while ((size = (int)redis.GetDatabase().Execute("DBSIZE")) < iterations + 1 && max-- > 0)
            {
                await Task.Delay(50);
            }


            var setTime = (double)end / Stopwatch.Frequency * 1000;
            var postProcessingTime = (double)start.ElapsedTicks / Stopwatch.Frequency * 1000;
            Console.WriteLine($"Max: {max}, size: {size}");
            Console.WriteLine($"TTA: {setTime / iterations} ms/cache storage operation {setTime} ms/{iterations} items");
            Console.WriteLine($"Postprocessing {iterations} items took {postProcessingTime} ms, {postProcessingTime / iterations} ms/item");
            Console.WriteLine($"Complete throughput to redis: {iterations / (setTime + postProcessingTime)} items/ms");
        }
    }
}
