﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace RedisSample {
	class Program {
        private static int mCacheItems = 1000;
        // Split a string that has white spaces, unless they are enclosed within "quotes"?
        private static Regex re = new Regex(@"['].+?[']|[^ ]+");

        static void Main(string[] args) {
			RedisConnectorHelper.Init(new Lazy<ConnectionMultiplexer>(() => {
				ConnectionMultiplexer.SetFeatureFlag("preventthreadtheft", true);
				//return ConnectionMultiplexer.Connect("localhost");
				return ConnectionMultiplexer.Connect("127.0.0.1:6379");
			}));
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

			var program = new Program();
            WriteHelp();
            while (true) {
                Thread.Sleep(500);
                Console.Write("> ");
                string line = Console.ReadLine();
                string[] s = re.Matches(line).Cast<Match>().Select(m => m.Value.Trim('\'')).ToArray();
                string c = s[0].ToLower();

                switch (c) {
                    case "q":
                    case "x":
                        return;
                    case "h":
                        WriteHelp();
                        continue;
                    case "c":
                        program.SampleChache(program);
                        continue;
                    case "s":
                        program.Subscribe();
                        continue;
                    case "p":
                        program.Publish();
                        continue;
                    case "w":
                        if (s.Length == 3) {
                            sw.Restart();
                            program.WriteKeyValue(s[1], s[2]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                        } else if (s.Length == 4 && int.TryParse(s[3], out _)) {
                            sw.Restart();
                            program.WriteKeyValue(s[1], s[2], int.Parse(s[3]));
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }                        
                        continue;
                    case "r":
                        if (s.Length == 2) {
                            sw.Restart();
                            string rt = program.ReadKeyValue(s[1]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                            Console.WriteLine($"Return value: \"{rt ?? "NULL"}\"");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "d":
                        if (s.Length == 2) {
                            sw.Restart();
                            bool rt = program.DeleteKey(s[1]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                            Console.WriteLine($"{(rt ? $"Deleted Key {s[1]}" : $"Key {s[1]} does not exist.")}");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "ttl":
                        if (s.Length == 2) {
                            program.GetKeyTTL(s[1]);                            
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "ex":
                        if (s.Length == 3) {
                            if (int.TryParse(s[2], out _)) {
                                program.SetKeyExpiration(s[1], int.Parse(s[2]));
                            } else {
                                Console.WriteLine("Invalid argument. Invalid argument for expiration seconds. Type h for help. ");
                            }
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "sw":
                        if (s.Length == 3) {
                            sw.Restart();
                            program.SetWriteKeyValue(s[1], s[2]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "sg":
                        if (s.Length == 2) {
                            sw.Restart();
                            List<string> rt = program.SetGetKeyValues(s[1]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                            if (rt.Count > 0) {
                                Console.WriteLine($"Return value: \r\n{rt.Aggregate((i, j) => i + "\r\n" + j)}");
                            } else {
                                Console.WriteLine($"Returns an empty set of values.");
                            }
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "sr":
                        if (s.Length == 2) {
                            sw.Restart();
                            List<string> rt = program.SetReadKeyValues(s[1]);
                            sw.Stop(); Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N0} ms");
                            if (rt.Count > 0) {
                                Console.WriteLine($"Return value: \r\n{rt.Aggregate((i, j) => i + "\r\n" + j)}");
                            } else {
                                Console.WriteLine($"Returns an empty set of values.");
                            }
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    default:
                        Console.WriteLine("Invalid Selection...");
                        WriteHelp();
                        continue;
                }
            }
        }

        public static void WriteHelp() {
            //Console.WriteLine("Select option:");
            //Console.WriteLine("  w Write cache         s Subscribe");
            //Console.WriteLine("  r Read cache          p Publish");
            //Console.WriteLine("  q Quit                h Help");
            //Console.WriteLine();            
            Console.WriteLine("  w  '<key>' '<value>' <timeout sec>       | Write key value with expiration");
            Console.WriteLine("  r  '<key>'                               | Read key value ");
            Console.WriteLine("  d  '<key>'                               | Delete key ");
            Console.WriteLine("  ttl  '<key>'                             | Read key time to live (expiration)");                        
            Console.WriteLine("  sw '<key>' '<value to add into the set>' | Add value to a set");
            Console.WriteLine("  sg '<key>'                               | Pop set values");
            Console.WriteLine("  sr '<key>'                               | Read set values");
            Console.WriteLine("  ex '<key>' <secondsFromNowToLive>        | Set expiration for a key (storing any type)");
            Console.WriteLine();
            Console.WriteLine("  q Quit                h Help");

        }

        public void SampleChache(Program program) {
            Console.WriteLine("Saving random data in cache");
            program.SaveBigData();

            Console.WriteLine("Reading data from cache");
            program.ReadData();
        }

        static int n = 1;
        public void Publish() {
            //Console.WriteLine("Connecting...");
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //Console.WriteLine("Publishing to channel messages...");
            cache.Publish("messages", new RedisValue($"Message #{n++}"));
        }

        public void Subscribe() {
            Task.Run(() => {
                Console.WriteLine("Subscriber starting... Subscribing to channel messages...");
                var cache = RedisConnectorHelper.Connection.GetDatabase();
                var channel = cache.Multiplexer.GetSubscriber().Subscribe("messages");
                channel.OnMessage(message => {
                    Console.WriteLine("[Recieved]------->" + (string)message.Message);
                });
                Console.WriteLine("Subscriber exits...");
            });
        }

        public void ReadData() {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            var devicesCount = mCacheItems;
            for (int i = 0; i < devicesCount; i++) {
                var value = cache.StringGet($"Device_Status:{i}");
                // Console.WriteLine($"Valor={value}");
                if (i == 549 || i == 233) Console.WriteLine($"Value={value}");
            }            
        }

        public void SaveBigData() {
            var devicesCount = mCacheItems;
            var rnd = new Random();
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            for (int i = 1; i < devicesCount; i++) {
                var value = rnd.Next(0, 10000);
                cache.StringSet($"Device_Status:{i}", value);
            }
        }

        // write / read / delete string values
        public void WriteKeyValue(string key, string value, int timeoutSec = -1) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            if (timeoutSec == -1) timeoutSec = 5 * 60;
            cache.StringSet(key, value, TimeSpan.FromSeconds(timeoutSec));
        }
        public string ReadKeyValue(string key) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            var val = cache.StringGet(key);
            return val.HasValue ? val.ToString() : "";
        }
        public bool DeleteKey(string key) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //var val = cache.StringGetDelete(key);
            //return val == RedisValue.Null ? false : true;
            return cache.KeyDelete(key);            
        }

        public static DateTime mRefDate = new DateTime(2000, 1, 1);

        // write / read / delete string sets per key
        public void SetWriteKeyValue(string key, string value) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            try {
                cache.SortedSetAdd(key, new RedisValue(value), (DateTime.Now - mRefDate).TotalSeconds, CommandFlags.None);                
            } catch (StackExchange.Redis.RedisServerException ex) {
                Console.WriteLine($"Error: key '{key}' is already used by other type. {ex.Message}");
			} catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public List<string> SetGetKeyValues(string key) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            List<string> l = new List<string>();
            while (true) {
                var v = cache.SortedSetPop(key);
                if (v != null) {
                    l.Add(v.Value.Element.ToString());
                } else {
                    break;
                }
            }
            return l;
        }
        public List<string> SetReadKeyValues(string key) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            List<string> l = new List<string>();
            var vals = cache.SortedSetRangeByScore(key);
            foreach (RedisValue v in vals) {
                l.Add(v.ToString());
            }
            return l;
        }
        public void SetKeyExpiration(string key, int expirationInSeconds) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            try {
                cache.KeyExpire(key, DateTime.Now.AddSeconds(expirationInSeconds));
                TimeSpan? ts = cache.KeyTimeToLive(key);
                if (ts != null) {
                    Console.WriteLine($"Key '{key}' TTL is {ts.Value.TotalSeconds:N1} sec");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        public void GetKeyTTL(string key) {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            try {                
                TimeSpan? ts = cache.KeyTimeToLive(key);
                if (ts != null) {
                    Console.WriteLine($"Key '{key}' TTL is {ts.Value.TotalSeconds:N1} sec");
                } else {
                    Console.WriteLine($"Error: Unknown Key '{key}'");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

    }
}
