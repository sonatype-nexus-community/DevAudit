using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet
{
    /// <summary>
    /// Interface for a matching operation performed against text.
    /// </summary>
    public interface IMatch : IResult
    {
        /// <summary>
        /// Execute the match operation against the text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        bool Execute(string text);
        /// <summary>
        /// The query for the match operation.
        /// </summary>
        string Query { get; }
    }
}
