using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Re = System.Text.RegularExpressions;

namespace ExpectNet.NET
{
    class RegexMatch : Match
    {
        private Re.Regex RegEx;

        public RegexMatch(string query) : base(query)
        {
            this.RegEx = new Re.Regex(query, Re.RegexOptions.Singleline);
        }

        public override bool Execute(string text)
        {
            this._Text = text;
            Re.Match m = this.RegEx.Match(text);
            this._IsMatch = m.Success;
            this._Result = this._IsMatch ? m : null;
            this._Count = this._IsMatch ? m.Groups.Count : 0;
            return this._IsMatch;           
        }
    }
}
