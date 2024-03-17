﻿using System;
using System.Text.RegularExpressions;
using hzcache;
using StackExchange.Redis;
using Utf8Json;

namespace RedisBackplane
{
    public class RedisBackplanceMemoryMemoryCacheOptions : HzCacheOptions
    {
        public string redisConnectionString { get; set; }
        public string applicationCachePrefix { get; set; }
        public string instanceId { get; set; }
    }

    public class RedisBackplaneMemoryHzCache : IHzCache
    {
        private readonly IHzCache hzCache;
        private readonly string instanceId = Guid.NewGuid().ToString();

        public RedisBackplaneMemoryHzCache(RedisBackplanceMemoryMemoryCacheOptions options)
        {
            var redis = ConnectionMultiplexer.Connect(options.redisConnectionString);
            hzCache = new hzcache.HzMemoryCache(new HzCacheOptions
            {
                evictionPolicy = options.evictionPolicy,
                cleanupJobInterval = options.cleanupJobInterval,
                valueChangeListener = (key, changeType, checksum, timestamp, isRegexp) =>
                {
                    options.valueChangeListener?.Invoke(key, changeType, checksum, timestamp, isRegexp);
                    var redisChannel = new RedisChannel(options.applicationCachePrefix, RedisChannel.PatternMode.Auto);
                    var messageObject = new RedisInvalidationMessage(instanceId, key, checksum, timestamp, isRegexp);
                    redis.GetSubscriber().PublishAsync(redisChannel, new RedisValue(JsonSerializer.ToJsonString(messageObject)));
                    Console.WriteLine("NotifyValueChange for " + key + " on instance " + instanceId);
                },
                defaultTTL = options.defaultTTL
            });

            if (string.IsNullOrWhiteSpace(options.redisConnectionString))
            {
                throw new ArgumentNullException("Redis connection string is required");
            }

            if (string.IsNullOrWhiteSpace(options.applicationCachePrefix))
            {
                throw new ArgumentNullException("Application cache prefix is required");
            }

            if (!string.IsNullOrWhiteSpace(options.instanceId))
            {
                instanceId = options.instanceId;
            }

            redis.GetSubscriber().Subscribe(options.applicationCachePrefix, (channel, message) =>
            {
                var invalidationMessage = JsonSerializer.Deserialize<RedisInvalidationMessage>(message.ToString());
                Console.WriteLine("["+instanceId+"] Received message for key "+invalidationMessage.key+ " from "+invalidationMessage.instanceId);

                if (invalidationMessage.instanceId != instanceId)
                {
                    hzCache.Remove(invalidationMessage.key, false, chksum => chksum == invalidationMessage.checksum);
                }
            });
        }

        public void RemoveByRegex(Regex re, bool sendNotification = true)
        {
            hzCache.RemoveByRegex(re, sendNotification);
        }

        public void EvictExpired()
        {
            hzCache.EvictExpired();
        }

        public void Clear()
        {
            hzCache.Clear();
        }

        public T Get<T>(string key) where T : class
        {
            return hzCache.Get<T>(key);
        }

        public void Set<T>(string key, T value) where T : class
        {
            hzCache.Set(key, value);
        }

        public void Set<T>(string key, T value, TimeSpan ttl) where T : class
        {
            hzCache.Set(key, value, ttl);
        }

        public T GetOrSet<T>(string key, Func<string, T> valueFactory, TimeSpan ttl) where T : class
        {
            return hzCache.GetOrSet(key, valueFactory, ttl);
        }

        public bool Remove(string key, bool sendNotification = true, Func<string, bool>? removeValidator = null)
        {
            return hzCache.Remove(key, sendNotification, removeValidator);
        }
    }
}
