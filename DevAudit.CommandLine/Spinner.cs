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
                thread.Start();
        }

        public void Stop()
        {
            active = false;
            Draw(' ');
        }

        private void Spin()
        {
            while (active)
            {
                Turn();
                Thread.Sleep(delay);
            }
        }

        private void Draw(char c)
        {
            Console.Write(c);
            try
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
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
            Stop();
        }
    }
}
