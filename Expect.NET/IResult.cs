using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpectNet
{
    public interface IResult
    {
        bool IsMatch { get; }
        object Result { get; }
        string Text { get; }
        int? Count { get; }
    }
}
