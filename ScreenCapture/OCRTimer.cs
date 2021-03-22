using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoLOCRHub
{
    class CustomTimer
    {
        private List<Action> delegates;
        private static readonly CustomTimer instance = new CustomTimer();
        private int interval;

        private bool running = false;

        static CustomTimer()
        {

        }

        private CustomTimer()
        {
            delegates = new List<Action>();
        }

        public void SetInterval(int Interval)
        {
            this.interval = Interval;
        }

        public void Start()
        {
            Logging.Verbose("Starting Timer");
            running = true;
            _ = Task.Run(() =>
            {
                while (running)
                {
                    delegates.ForEach(f => f());
                    Thread.Sleep(TimeSpan.FromMilliseconds(interval));
                }
            });
        }


        public void Stop()
        {
            running = false;
        }

        public void AddDelegate(Action d)
        {
            delegates.Add(d);
        }

        public void ClearDelegates()
        {
            delegates.Clear();
        }


        public static CustomTimer Instance
        {
            get { return instance; }
        }
    }
}
