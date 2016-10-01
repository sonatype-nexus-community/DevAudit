using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExpectNet
{
    public interface ISpawnable
    {
        void Init();

        void Write(string command);

        string Read(CancellationTokenSource cts);

        Task<string> ReadAsync();
    }
}
