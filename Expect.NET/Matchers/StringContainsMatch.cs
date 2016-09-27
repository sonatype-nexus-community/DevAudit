using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpectNet.NET
{
    class StringContainsMatch : Match
    {
        public StringContainsMatch(string query) : base(query) {}

        public override bool Execute(string text)
        {
            this._IsMatch =  text.Contains(Query);
            this._Text = text;
            this._Result = this._IsMatch ? text : null;
            this._Count = this._IsMatch ? 1 : 0;
            return this._IsMatch;
        }
    }
}
