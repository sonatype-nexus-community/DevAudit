using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevAudit.CommandLine
{
    //Modified from http://stackoverflow.com/a/33685254
    public class Spinner : IDisposable
    {
        //private const string Sequence = @"/-\|";
        private const string Sequence = @"|/-\|/-\";
        private int counter = 0;
        private readonly int left;
        private readonly int top;
        private readonly int delay;
        private bool active;
        private readonly Thread thread;
        public EventWaitHandle wh = new AutoResetEvent(true);

        public Spinner(int left, int top, int delay = 100)
        {
            this.left = left;
            this.top = top;
            this.delay = delay;
            thread = new Thread(Spin);
        }

        public Spinner(int delay = 100)
        {
            this.delay = delay;
            thread = new Thread(Spin);
        }

        public void Start()
        {
            active = true;
            if (!thread.IsAlive)
            {
                thread.Start();
            }
        }

        public void Pause()
        {
            Draw(' ');
            if (Console.CursorLeft > 0)
            {
                Draw('\n');
            }
            wh.Reset();
        }

        public void UnPause()
        {
            wh.Set();
        }

        public void Stop()
        {
            active = false;
            Draw(' ');
            if (Console.CursorLeft > 0)
            {
                Draw('\n');
            }
        }

        private void Spin()
        {
            wh.WaitOne();
            while (active)
            {
                Turn();
                Thread.Sleep(delay);
            }
            wh.Dispose();
        }

        private void Draw(char c)
        {
            Console.Write(c);
            try
            {
                Console.SetCursorPosition(Console.CursorLeft > 0 ? Console.CursorLeft - 1 : 0, Console.CursorTop);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
        }

        private void Turn()
        {
            Draw(Sequence[++counter % Sequence.Length]);
        }

        public void Dispose()
        {
            if (active)
            {
                Stop();
            }
        }
    }
}
