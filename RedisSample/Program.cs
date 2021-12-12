using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using StackExchange.Redis;

namespace RedisSample {
	class Program {
        private static int mCacheItems = 1000;

        static void Main(string[] args) {
            var program = new Program();
            WriteHelp();
            while (true) {
                Thread.Sleep(500);
                Console.Write("Enter Selection: ");
                string line = Console.ReadLine();
                switch (line.ToLower()) {
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
                    default:
                        Console.WriteLine("Invalid Selection...");
                        WriteHelp();
                        continue;
                }
            }
        }

        public static void WriteHelp() {
            Console.WriteLine("Select option:");
            Console.WriteLine("  c Sample cache        s Subscribe");
            Console.WriteLine("  s Subscribe           p Publish");
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
    }
}
