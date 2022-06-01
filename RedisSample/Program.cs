using System;
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
                            program.WriteKeyValue(s[1], s[2]);
                        } else if (s.Length == 4 && int.TryParse(s[3], out _)) {
                            program.WriteKeyValue(s[1], s[2], int.Parse(s[3]));
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }                        
                        continue;
                    case "r":
                        if (s.Length == 2) {
                            string rt = program.ReadKeyValue(s[1]);
                            Console.WriteLine($"Return value: \"{rt ?? "NULL"}\"");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "d":
                        if (s.Length == 2) {
                            bool rt = program.DeleteKey(s[1]);
                            Console.WriteLine($"{(rt ? $"Deleted Key {s[1]}" : $"Key {s[1]} does not exist.")}");
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "sw":
                        if (s.Length == 3) {
                            program.SetWriteKeyValue(s[1], s[2]);
                        } else {
                            Console.WriteLine("Invalid argument. Type h for help. ");
                        }
                        continue;
                    case "sg":
                        if (s.Length == 2) {
                            List<string> rt = program.SetGetKeyValues(s[1]);
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

            Console.WriteLine("Writing/Reading key/value:");
            Console.WriteLine("  w  '<key>' '<value>' <timeout sec>");
            Console.WriteLine("  r  '<key>'");            

            Console.WriteLine();
            Console.WriteLine("Writing/Getting values into a sorted set by key:");
            Console.WriteLine("  sw '<key>' '<value to add into the set>'");
            Console.WriteLine("  sg '<key>'");

            Console.WriteLine(); 
            Console.WriteLine("Delete key:");
            Console.WriteLine("  d '<key>'");

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

    }
}
