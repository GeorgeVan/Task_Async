using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace RxStudy
{
    //https://msdn.microsoft.com/en-us/library/hh242970(v=vs.103).aspx
    class Program
    {
        static void Main(string[] args)
        {
            //Test1();
            //Test2();
            //Test3();
            //Test4();
            //Test5();
            //Test6HotCold();
            //Test7();
            //Test8();
            //Test10();
            //Test11();
            //Test12();
            //Test13();
            //Test15();
            //Test16();

            // Test0();
            //Test00();
            //Test18();
            //Test19();
            //Test20();
            //Test21();
            Test22();
        }

        public class TimeIt : IDisposable
        {
            private readonly string _name;
            private readonly Stopwatch _watch;
            public TimeIt(string name)
            {
                _name = name;
                _watch = Stopwatch.StartNew();
            }
            public void Dispose()
            {
                _watch.Stop();
                Console.WriteLine("{0} took {1}", _name, _watch.Elapsed);
            }
        }

        //测试 using
        static void Test22()
        {
            Observable.Using( () => new TimeIt("Subscription Timer"),
                             (x) => Observable.Interval(TimeSpan.FromSeconds(1)))
            .Take(5).Dump("Using");

            Console.ReadKey();

        }

        private static IObservable<long> GetSubValues(long offset)
        {
            //Produce values [x*10, (x*10)+1, (x*10)+2] 4 seconds apart, but starting immediately.
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(4))
            .Select(x => (offset * 10) + x)
            .Take(3);
        }

        static void Test20()
        {
            Observable
                .Interval(TimeSpan.FromSeconds(3))
                .Select(i => i + 1) //Values start at 0, so add 1.
                .Take(3)            //We only want 3 values
                .SelectMany(GetSubValues) //project into child sequences
                .Dump("SelectMany");
            Console.ReadKey();

        }

        static void Test20_1()
        {
/*            var query = from i in Observable.Interval(TimeSpan.FromSeconds(3))
                        top 3
                        select i+1
                        from j in 
                .Select(i => i + 1) //Values start at 0, so add 1.
                .Take(3)            //We only want 3 values
                .SelectMany(GetSubValues) //project into child sequences
                .Dump("SelectMany");
            Console.ReadKey();*/

        }

        static void Test21()
        {
            var query = from i in Observable.Range(1, 5)
                        where i % 2 == 0
                        from j in GetSubValues(i)
                        select new { i, j };
            query.Dump("SelectMany");
            Console.ReadKey();
        }

        public async static Task<string> GetHelloString()
        {
            await Task.Delay(2000);
            return "Hello";
        }
        public static async Task<string> GetWorldString()
        {
            await Task.Delay(3000);
            return "World";
        }

        public async static Task<string> WaitForFirstResultAndReturnResultWithTimeOut2()
        {
            Task<string> task1 = GetHelloString();
            Task<string> task2 = GetWorldString();
            return await await Task.WhenAny(task1, task2)
                .ToObservable()
                .Timeout(TimeSpan.FromMilliseconds(5000))
                .FirstAsync();

        }

        static void Test19()
        {
            try
            {
                String s = WaitForFirstResultAndReturnResultWithTimeOut2().GetAwaiter().GetResult();
                Console.WriteLine("Ended.: " + s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }

        static void Test17()
        {
            Subject<int> subject = new Subject<int>();
            var subscription = subject.Subscribe(
                                     x => Console.WriteLine("Value published: {0}", x),
                                     () => Console.WriteLine("Sequence Completed."));
            subject.OnNext(1);
            subject.OnNext(2);



            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            subject.OnCompleted();
            subscription.Dispose();
            Console.ReadKey();
        }


        static void Test18()
        {
            Subject<long> subject = new Subject<long>();
            var subSource = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(subject);
            //最上游

            var subSubject1 = subject.Subscribe(
                                     x => Console.WriteLine("Value published to observer #1: {0} @{1}", x, Thread.CurrentThread.ManagedThreadId),
                                     () => Console.WriteLine("Sequence Completed.@{0}", Thread.CurrentThread.ManagedThreadId));
            var subSubject2 = subject.Subscribe(
                                     x => Console.WriteLine("Value published to observer #2: {0} @{1}", x, Thread.CurrentThread.ManagedThreadId),
                                     () => Console.WriteLine("Sequence Completed.@{0}", Thread.CurrentThread.ManagedThreadId));

            Console.WriteLine("Press any key to continue @{0}", Thread.CurrentThread.ManagedThreadId);
            Console.ReadKey();
            subject.OnCompleted();
            subSubject1.Dispose();
            subSubject2.Dispose();
            Console.ReadKey();
        }
        static void Test00()
        {
            FileSearcher lister = new FileSearcher();
            lister.FileFound += (sender, eventArgs) => Console.WriteLine(eventArgs.FoundFile);
            lister.DirectoryChanged += (sender, eventArgs) =>
            {
                Console.Write($"Entering '{eventArgs.CurrentSearchDirectory}'.");
                Console.WriteLine($" {eventArgs.CompletedDirs} of {eventArgs.TotalDirs} completed...");
            };

            lister.Search(@"c:\", "*.*", false);
            Console.Read();
        }

        //测试delegate如果绑定了多个函数，返回值怎么办？
        //虽然每个都调用，但是只返回最后一个
        //可以通过GetInvocationList DynamicInvoke来一个个调用。
        static void Test0()
        {
            Func<String, int> ToInt = null;
            ToInt += (a) => { Console.WriteLine("Calling 1: " + a); return 1; };

            Console.WriteLine(ToInt("aaa"));

            ToInt += (a) => { Console.WriteLine("Calling 2: " + a); return 2; };
            Console.WriteLine(ToInt("aaa"));

            var xx = ToInt.Invoke("ddd");
            Console.WriteLine(xx);


            var vv = ToInt.GetInvocationList().Select(a => a.DynamicInvoke("eee")).ToArray();
            vv.ToList().ForEach(v => Console.WriteLine(v));

            Console.ReadKey();
        }

        static void Test1()
        {
            IObservable<int> source = Observable.Range(1, 5); //creates an observable sequence of 5 integers, starting from 1
            IDisposable subscription = source.Subscribe(
                                        x => Console.WriteLine("OnNext: {0}", x), //prints out the value being pushed
                                        ex => Console.WriteLine("OnError: {0}", ex.Message),
                                        () => Console.WriteLine("OnCompleted"));
            Console.ReadKey();
        }

        static void Test2()
        {
            IObservable<int> source = Observable.Range(1, 10);
            IObserver<int> obsvr = Observer.Create<int>(
                x => Console.WriteLine("OnNext: {0}", x),
                ex => Console.WriteLine("OnError: {0}", ex.Message),
                () => Console.WriteLine("OnCompleted"));
            IDisposable subscription = source.Subscribe(obsvr);
            Console.WriteLine("Press ENTER to unsubscribe...");
            Console.ReadLine();
            subscription.Dispose();
        }

        static void Test3()
        {
            Console.WriteLine("Current Time: " + DateTime.Now);

            var source = Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1))
                                   .Timestamp();
            using (source.Subscribe(x => Console.WriteLine("{0}: {1}", x.Value, x.Timestamp)))
            {
                Console.WriteLine("Press any key to unsubscribe");
                Console.ReadKey();
            }
        }

        static void Test4()
        {
            IEnumerable<int> e = new List<int> { 1, 2, 3, 4, 5 };

            IObservable<int> source = e.ToObservable();
            IDisposable subscription = source.Subscribe(
                                        x => Console.WriteLine("OnNext: {0}", x),
                                        ex => Console.WriteLine("OnError: {0}", ex.Message),
                                        () => Console.WriteLine("OnCompleted"));
            Console.ReadKey();
        }

        static void Test5Cold()
        {
            IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1));

            IDisposable subscription1 = source.Subscribe(
                            x => Console.WriteLine("Observer 1: OnNext: {0}", x),
                            ex => Console.WriteLine("Observer 1: OnError: {0}", ex.Message),
                            () => Console.WriteLine("Observer 1: OnCompleted"));

            IDisposable subscription2 = source.Subscribe(
                            x => Console.WriteLine("Observer 2: OnNext: {0}", x),
                            ex => Console.WriteLine("Observer 2: OnError: {0}", ex.Message),
                            () => Console.WriteLine("Observer 2: OnCompleted"));

            Console.WriteLine("Press any key to unsubscribe");
            Console.ReadKey();
            subscription1.Dispose();
            subscription2.Dispose();
        }

        static void Test6Hot()
        {
            Console.WriteLine("Current Time: " + DateTime.Now);
            var source = Observable.Interval(TimeSpan.FromSeconds(1));            //creates a sequence

            IConnectableObservable<long> hot = Observable.Publish<long>(source);  // convert the sequence into a hot sequence

            IDisposable subscription1 = hot.Subscribe(                        // no value is pushed to 1st subscription at this point
                                        x => Console.WriteLine("Observer 1: OnNext: {0}", x),
                                        ex => Console.WriteLine("Observer 1: OnError: {0}", ex.Message),
                                        () => Console.WriteLine("Observer 1: OnCompleted"));
            Console.WriteLine("Current Time after 1st subscription: " + DateTime.Now);
            Thread.Sleep(3000);  //idle for 3 seconds
            hot.Connect();       // hot is connected to source and starts pushing value to subscribers 
            Console.WriteLine("Current Time after Connect: " + DateTime.Now);
            Thread.Sleep(3000);  //idle for 3 seconds
            Console.WriteLine("Current Time just before 2nd subscription: " + DateTime.Now);

            IDisposable subscription2 = hot.Subscribe(     // value will immediately be pushed to 2nd subscription
                                        x => Console.WriteLine("Observer 2: OnNext: {0}", x),
                                        ex => Console.WriteLine("Observer 2: OnError: {0}", ex.Message),
                                        () => Console.WriteLine("Observer 2: OnCompleted"));
            Console.ReadKey();
        }


        static void Test7()
        {
            var lbl = new Label();
            var frm = new Form { Controls = { lbl } };
            frm.MouseMove += (sender, args) =>
            {
                lbl.Text = args.Location.ToString();
            };
            Application.Run(frm);
        }

        static void Test8()
        {
            var lbl = new Label();
            var frm = new Form { Controls = { lbl } };

            Observable.FromEventPattern<MouseEventArgs>(frm, "MouseMove")
            .Sample(TimeSpan.FromMilliseconds(50))
            .ObserveOn(frm)
            .Subscribe(evt =>
            {
                lbl.Text = evt.EventArgs.Location.ToString();
            });

            Application.Run(frm);
        }


        static void Test9()
        {
            Stream inputStream = Console.OpenStandardInput();
            byte[] someBytes = new byte[10];
            IObservable<int> source = inputStream.ReadAsync(someBytes, 0, 10).ToObservable();
            IDisposable subscription = source.Subscribe(
                                        x => Console.WriteLine("OnNext: {0}", x),
                                        ex => Console.WriteLine("OnError: {0}", ex.Message),
                                        () => Console.WriteLine("OnCompleted"));
            Console.ReadKey();
        }

        static void Test10()
        {
            {
                var source1 = Observable.Range(1, 3);
                var source2 = Observable.Range(1, 3);
                source1.Concat(source2)
                       .Subscribe(Console.WriteLine);
                Console.ReadKey();
            }
            {
                var source1 = Observable.Range(1, 3);
                var source2 = Observable.Range(1, 3);
                source1.Merge(source2)
                       .Subscribe(Console.WriteLine);
                Console.ReadKey();
            }

            {
                var source1 = Observable.Range(1, 3);
                var source2 = Observable.Range(4, 3);
                source1.Catch(source2)
                       .Subscribe(Console.WriteLine);
                Console.ReadKey();
            }

            {
                var source1 = Observable.Throw<int>(new Exception("An error has occurred."));
                var source2 = Observable.Range(4, 3);
                source1.OnErrorResumeNext(source2)
                       .Subscribe(Console.WriteLine);
                Console.ReadKey();
            }
            {
                var seqNum = Observable.Range(1, 5);
                var seqString = from n in seqNum
                                select new string('*', (int)n);
                seqString.Subscribe(str => { Console.WriteLine(str); });
                Console.ReadKey();
            }
        }

        //鼠标位置打印在控制台 Projection
        static void Test11()
        {
            var frm = new Form();
            IObservable<EventPattern<MouseEventArgs>> move = Observable.FromEventPattern<MouseEventArgs>(frm, "MouseMove");
            //这里的原理是利用Reflection从frm里面找到MouseMove的挂接点

            IObservable<System.Drawing.Point> points = from evt in move.Sample(TimeSpan.FromSeconds(3))
                                                       select evt.EventArgs.Location;
            points.Subscribe(pos => Console.WriteLine("mouse at " + pos));
            Application.Run(frm);

        }

        //鼠标位置打印在控制台 Projection
        static void Test11_1()
        {
            var frm = new Form();
            IObservable<EventPattern<MouseEventArgs>> move = Observable.FromEventPattern<MouseEventArgs>(frm, "MouseMove");
            IObservable<System.Drawing.Point> points = from evt in move
                                                       select evt.EventArgs.Location;
            points.Subscribe(pos => Console.WriteLine("mouse at " + pos));
            Application.Run(frm);

        }

        /*
         * In the following example, we first create a source sequence which produces an integer every 5 seconds, 
         * and decide to just take the first 2 values produced (by using the Take operator). We then use SelectMany 
         * to project each of these integers using another sequence of {100, 101, 102}. By doing so, two mini observable
         *  sequences are produced, {100, 101, 102} and {100, 101, 102}. These are finally flattened into a single 
         *  stream of integers of {100, 101, 102, 100, 101, 102} and pushed to the observer.
         */
        static void Test12()
        {
            var source1 = Observable.Interval(TimeSpan.FromSeconds(5)).Take(2);
            var proj = Observable.Range(100, 3);
            var resultSeq = source1.SelectMany(proj);

            var sub = resultSeq.Subscribe(x => Console.WriteLine("OnNext : {0}", x.ToString()),
                                          ex => Console.WriteLine("Error : {0}", ex.ToString()),
                                          () => Console.WriteLine("Completed"));
            Console.ReadKey();
        }

        static void Test13()
        {
            IObservable<int> seq = Observable.Generate(0, i => i < 10, i => i + 1, i => i * i);
            IObservable<int> source = from n in seq
                                      where n < 5
                                      select n;
            source.Subscribe(x => { Console.WriteLine(x); });   // output is 0, 1, 4, 9
            Console.ReadKey();
        }

        static void Test15()
        {
            var frm = new Form();
            IObservable<EventPattern<MouseEventArgs>> move = Observable.FromEventPattern<MouseEventArgs>(frm, "MouseMove");
            IObservable<System.Drawing.Point> points = from evt in move
                                                       select evt.EventArgs.Location;
            var overfirstbisector = from pos in points
                                    where pos.X == pos.Y
                                    select pos;
            var movesub = overfirstbisector.Subscribe(pos => Console.WriteLine("mouse at " + pos));
            Application.Run(frm);

        }

        static void Test16()
        {
            {
                var seq = Observable.Interval(TimeSpan.FromSeconds(1));
                var bufSeq = seq.Buffer(5);
                var sub = bufSeq.Subscribe(values => Console.WriteLine(values.Sum()));
                Console.ReadKey();
                sub.Dispose();
            }
            {
                var seq = Observable.Interval(TimeSpan.FromSeconds(1));
                var bufSeq = seq.Buffer(TimeSpan.FromSeconds(3));
                bufSeq.Subscribe(value => Console.WriteLine(value.Sum()));
                Console.ReadKey();
            }

        }
    }

    public static class SampleExtentions
    {
        public static void Dump<T>(this IObservable<T> source, string name)
        {
            source.Subscribe(
            i => Console.WriteLine("{0}-->{1}", name, i),
            ex => Console.WriteLine("{0} failed-->{1}", name, ex.Message),
            () => Console.WriteLine("{0} completed", name));
        }
    }

}
