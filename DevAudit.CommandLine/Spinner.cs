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
        #region Private fields
        private const string sequence = @"|/-\|/-\";
        private int counter = 0;
        private readonly int left;
        private readonly int top;
        private readonly int delay;
        private bool active;
        private readonly Thread thread;
        #endregion

        #region Public fields
        public readonly EventWaitHandle wh = new AutoResetEvent(true);
        #endregion

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

        public void Pause(bool break_line = true)
        {
            Draw(' ');
            if (Console.CursorLeft > 0 && break_line)
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
            wh.Dispose(); //Dispose of the wait handle right before the thread exits.
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

            Draw(sequence[++counter % sequence.Length]);
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
