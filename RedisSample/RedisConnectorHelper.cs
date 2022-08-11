using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisSample {
    public class RedisConnectorHelper {
		//static RedisConnectorHelper() {
		//	RedisConnectorHelper.lazyConnection = new Lazy<ConnectionMultiplexer>(() => {
		//		//return ConnectionMultiplexer.Connect("localhost");
		//		return ConnectionMultiplexer.Connect("127.0.0.1:6379");
		//	});
		//}

		public static bool Init(Lazy<ConnectionMultiplexer> a) {
			if (mIsinitiated) return false;
			RedisConnectorHelper.lazyConnection = a;
			mIsinitiated = true;
			return true;
		}

		private static Lazy<ConnectionMultiplexer> lazyConnection;
		private static bool mIsinitiated = false;

		public static ConnectionMultiplexer Connection {
            get {
				return lazyConnection.Value;                
            }
        }
    }
}
