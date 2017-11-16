using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AsyncCache
{

    public class AsyncCache<TKey, TValue>
    {
        private readonly Func<TKey, Task<TValue>> _valueFactory; //这里只是一个函数，根据TKey返回不同的Task
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map; 
        public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
        {
            if (valueFactory == null) throw new ArgumentNullException("loader");
            _valueFactory = valueFactory;
            _map = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
        }
        public Task<TValue> this[TKey key]
        {
            get
            {
                if (key == null) throw new ArgumentNullException("key");
                return _map.GetOrAdd(key, toAdd =>
                new Lazy<Task<TValue>>(() => _valueFactory(toAdd))).Value;
                //如果没有这个key，则会调用第二个参数：Function
                //调用第二个就会生成一个Lazy的东东；
                //这个Lazy的东东在使用的时候再去调用这个Function：() => _valueFactory(toAdd)
                //这个Function 调用 _valueFactory(toAdd) 返回 Task<TValue>
            }
        }
    }

    class Program
    {
        private static AsyncCache<string, string> m_webPages = 
            new AsyncCache<string, string>(async (url)=> await new WebClient().DownloadStringTaskAsync(url));

        async static Task MainAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Console.WriteLine((await m_webPages["http://www.microsoft.com"]).Length +"  @ "+ stopwatch.Elapsed.Milliseconds  );
            stopwatch.Restart ();
            Console.WriteLine((await m_webPages["http://www.microsoft.com"]).Length + "  @ " + stopwatch.Elapsed.Milliseconds);
            stopwatch.Restart();
            Console.WriteLine((await m_webPages["http://www.microsoft.com"]).Length + "  @ " + stopwatch.Elapsed.Milliseconds);
        }

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
            Console.ReadKey();
        }
    }
}
