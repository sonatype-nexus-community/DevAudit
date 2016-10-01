using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renci.SshNet
{
    /// <summary>
    /// Abstract base class for IMatch implementations
    /// </summary>
    public abstract class Match : IMatch
    {
        #region Public properties
        /// <summary>
        /// Boolean indicating if the match was successful
        /// </summary>
        public bool IsMatch
        {
            get
            {
                return _IsMatch;
            }
        }

        /// <summary>
        /// The result of the match.
        /// </summary>
        public object Result
        {
            get
            {
                return this._Result;
            }
        }

        /// <summary>
        /// The text that will be queried for the expected string.
        /// </summary>
        public string Text
        {
            get
            {
                return this._Text;
            }
        }

        /// <summary>
        /// The number of matches found in the text.
        /// </summary>
        public int? Count
        {
            get
            {
                return this._Count;
            }
        }
        
        /// <summary>
        /// The expected string to query the text for.
        /// </summary>
        public string Query { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a match object with the expected string.
        /// </summary>
        /// <param name="query"></param>
        public Match (string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query");
            }
            this.Query = query;
        }
        #endregion

        #region Abstract methods
        /// <summary>
        /// Execute the match operation against text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public abstract bool Execute(string text);
        #endregion

        #region Protected and private fields
        /// <summary>
        /// Backing field for IsMatch property.
        /// </summary>
        protected bool _IsMatch = false;
        /// <summary>
        /// Backin fied for Result property.
        /// </summary>
        protected object _Result;
        /// <summary>
        /// Backing field for Text property. 
        /// </summary>
        protected string _Text;
        /// <summary>
        /// Backing field for Count property
        /// </summary>
        protected int? _Count;
        #endregion

        

    }
}
