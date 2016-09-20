using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpectNet.NET
{
    public abstract class Match : IMatch
    {
        #region Public properties
        public bool IsMatch
        {
            get
            {
                return _IsMatch;
            }
        }

        public object Result
        {
            get
            {
                return this._Result;
            }
        }

        public int? Count
        {
            get
            {
                return this._Count;
            }
        }

        public string Query { get; private set; }
        #endregion

        #region Constructors
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
        public abstract bool Execute(string text);
        #endregion

        #region Protected and private fields
        protected bool _IsMatch = false;
        protected object _Result;
        protected int? _Count;
        #endregion

        public static IMatch Contains(string query)
        {
            return new StringContainsMatch(query);
        }

    }
}
