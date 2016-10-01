using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renci.SshNet
{
    /// <summary>
    /// Interface for the result of an Expect operatiob
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Text matched the expected query.
        /// </summary>
        bool IsMatch { get; }
        /// <summary>
        /// Result object: can be string or a Regex.Match object etc.
        /// </summary>
        object Result { get; }
        /// <summary>
        /// The text the expected query was match against.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// The number of matches found in the text.
        /// </summary>
        int? Count { get; }
    }
}
