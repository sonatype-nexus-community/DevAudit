using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpectNet
{
    public interface ISpawnable
    {
        void Init();

        void Write(string command);

        string Read();

        System.Threading.Tasks.Task<string> ReadAsync();
    }
}
