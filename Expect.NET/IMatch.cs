using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpectNet
{
    public interface IMatch : IResult
    {
        bool Execute(string text);
        string Query { get; }
    }
}
