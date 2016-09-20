using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Re = System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExpectNet.NET;

namespace ExpectNet
{
    public class Session
    {
        private ISpawnable _spawnable;
        private StringBuilder OutputBuilder = new StringBuilder(1000);

        public int Timeout { get; set; } = 2500;

        public int Delay { get; set; } = 100;
        
        public string LineTerminator { get; set; }

        public string Output
        {
            get
            {
                return OutputBuilder.ToString();
            }
        }

        public Session(ISpawnable spawnable, string line_terminator)
        {
            _spawnable = spawnable;
            LineTerminator = line_terminator;
            Expect = new ExpectCommands(this);
            Send = new SendCommands(this);
        }

        public Session(ISpawnable spawnable, string line_terminator, int timeout) : this(spawnable, line_terminator)
        {
            this.Timeout = timeout;
        }

        private IResult _Expect(IMatch matcher, Action<string> handler, int timeout = 0, bool timeout_throws = false)
        {
            if (timeout == 0) timeout = this.Timeout;
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            IResult result = matcher;
            StringBuilder matchOutputBuilder = new StringBuilder();
            Task task = Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && !matcher.IsMatch)
                {
                    matchOutputBuilder.Append(_spawnable.Read());
                    if (!matcher.Execute(matchOutputBuilder.ToString()))
                    {
                        Thread.Sleep(Delay);
                    }
                }
            }, ct);
            if (task.Wait(timeout, ct))
            {
                handler?.Invoke(matchOutputBuilder.ToString());
                return result;
            }
            else
            {
                tokenSource.Cancel();
                if (timeout_throws)
                {
                    result = null;
                    throw new TimeoutException(string.Format("Timed out waiting for match for {0} in output {1}.", matcher.Query, matchOutputBuilder.ToString()));
                }
                else
                {
                    return result;
                }
            }
        }

        private IResult _Expect(IMatch matcher, Action<string> handler)
        {
            return this._Expect(matcher, handler, this.Timeout, false);
        }

        private IResult _Expect(IMatch matcher, Action<string> handler, int timeout, int retries)
        {
            IResult result = null; ;
            for (int i = 1; i <= retries; i++)
            {
                result = _Expect(matcher, handler, timeout);
                if (result.IsMatch) return result;
            }
            return result;
        }

        private bool _Expect(IMatch matcher, int timeout = 0)
        {
            if (timeout == 0) timeout = this.Timeout;
            IResult result = matcher;
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            StringBuilder matchOutputBuilder = new StringBuilder(1000);
            Task task = Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && !result.IsMatch)
                {
                    matchOutputBuilder.Append(_spawnable.Read());
                    if (!matcher.Execute(matchOutputBuilder.ToString()))
                    {
                        Thread.Sleep(Delay);
                    }
                }
            }, ct);
            if (task.Wait(timeout, ct))
            {
                return true;
            }
            else
            {
                tokenSource.Cancel();
                return false;
            }
        }

        private bool _Expect(IMatch matcher)
        {
            return this._Expect(matcher, this.Timeout);
        }


        private bool _Expect(IMatch matcher, int timeout, int retries)
        {
            bool result = false;
            for (int i = 1; i <= retries; i++)
            {
                result = _Expect(matcher, timeout);
                if (result) return result;
            }
            return result;
        }

        private List<IResult> _Expect(List<Tuple<IMatch, Action<string>>> matchers, int timeout = 0, bool timeout_throws = false)
        {
            if (timeout == 0) timeout = this.Timeout;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            List<Tuple<IResult, Action<string>>> results = matchers.Select(m => new Tuple<IResult, Action<string>>(m.Item1 as IResult, m.Item2)).ToList();
            StringBuilder matchOutputBuilder = new StringBuilder(1000);
            Task task = Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && results.Count(r => r.Item1.IsMatch) == 0)
                {
                    string output = _spawnable.Read();
                    matchOutputBuilder.Append(output);
                    foreach (Tuple<IResult, Action<string>> r in results)
                    {
                        (r.Item1 as IMatch).Execute(matchOutputBuilder.ToString());
                        if (r.Item1.IsMatch)
                        {
                            r.Item2?.Invoke(matchOutputBuilder.ToString());
                            break;
                        }
                    }
                    if (results.Count(r => r.Item1.IsMatch) == 0) Thread.Sleep(Delay);
                }
            }, ct);
            if (task.Wait(timeout, ct))
            {
                return results.Select(r => r.Item1).ToList();
            }
            else
            {
                tokenSource.Cancel();
                bool at_least_one_match = results.Any(r => r.Item1.IsMatch);
                if (!at_least_one_match && timeout_throws)
                {
                    results = null;
                    throw new TimeoutException(string.Format("Timed out waiting for at least one match for {0} in output {1}.", matchers.Select(m => m.Item1.Query).Aggregate((p, n) => { return p + "or" + n; }), matchOutputBuilder.ToString()));
                }
                return results.Select(r => r.Item1).ToList();
            }
        }

        private List<IResult> _Expect(List<Tuple<IMatch, Action<string>>> matchers, int timeout, int retries, bool timeout_throws = false)
        {
            List<IResult> result = null; ;
            for (int i = 1; i <= retries; i++)
            {
                result = _Expect(matchers, timeout);
                if (result.Any(r => r.IsMatch)) return result;
            }
            if (!timeout_throws)
            {
                throw new TimeoutException(string.Format("Timed out waiting for at least one match for {0}.", matchers.Select(m => m.Item1.Query).Aggregate((p, n) => { return p + "or" + n; })));
            }
            else
            {
                return result;
            }
        }

        private async Task<IResult> _ExpectAsync(IMatch matcher, Action<string> handler, int timeout = 0, bool timeout_throws = false)
        {
            IResult result = matcher;
            Task timeoutTask = null;
            if (timeout == 0) timeout = this.Timeout;
            if (timeout > 0)
            {
                timeoutTask = Task.Delay(timeout);
            }
            StringBuilder matchOutputBuilder = new StringBuilder(1000);
            Task<string> readTask = _spawnable.ReadAsync();
            IList<Task> tasks = new List<Task>();
            tasks.Add(readTask);
            if (timeoutTask != null)
            {
                tasks.Add(timeoutTask);
            }
            Task completed = await Task.WhenAny(tasks).ConfigureAwait(false);
            if (completed == readTask)
            {
                string output = _spawnable.Read();
                OutputBuilder.Append(output);
                matchOutputBuilder.Append(output);
                matcher.Execute(matchOutputBuilder.ToString());
                if (result.IsMatch)
                {
                    handler?.Invoke(matchOutputBuilder.ToString());
                }
            }
            else
            {
                if (timeout_throws) throw new TimeoutException(string.Format("Timed out waiting for match for {0} in output {1}.", matcher.Query, matchOutputBuilder.ToString())); ;
            }
            return result;
        }

        private async Task<IResult> _ExpectAsync(IMatch matcher, Action<string> handler)
        {
            return await this._ExpectAsync(matcher, handler, this.Timeout, false);
        }

        public ExpectCommands Expect { get; protected set; }

        public SendCommands Send { get; protected set; }

        public class ExpectCommands
        {
            private Session Session;

            internal ExpectCommands(Session parent)
            {
                if (ReferenceEquals(parent, null)) throw new ArgumentNullException("parent");
                this.Session = parent;
            }

            public IResult Contains(string query, Action<string> handler, int? timeout = null, int? retries = null)
            {
                return Session._Expect(new StringContainsMatch(query), handler, timeout.HasValue ? timeout.Value : this.Session.Timeout, retries.HasValue ? retries.Value : 1);
            }

            public List<IResult> ContainsEither(string query1, Action<string> handler1, string query2, Action<string> handler2, int? timeout = null, bool timeout_throws = false)
            {
                List<Tuple<IMatch, Action<string>>> q = new List<Tuple<IMatch, Action<string>>>()
                {
                    new Tuple<IMatch, Action<string>>(new StringContainsMatch(query1), handler1),
                    new Tuple<IMatch, Action<string>>(new StringContainsMatch(query2), handler2)
                };
                return Session._Expect(q, timeout.HasValue ? timeout.Value : this.Session.Timeout, timeout_throws);
            }

            public List<IResult> ContainsEither(string query1, Action<string> handler1, string query2, Action<string> handler2, int timeout, int retries, bool timeout_throws = false)
            {
                List<Tuple<IMatch, Action<string>>> q = new List<Tuple<IMatch, Action<string>>>()
                {
                    new Tuple<IMatch, Action<string>>(new StringContainsMatch(query1), handler1),
                    new Tuple<IMatch, Action<string>>(new StringContainsMatch(query2), handler2)
                };
                return Session._Expect(q, timeout, retries, timeout_throws);
            }

            public IResult Regex(string query, Action<string> handler, int? timeout = null)
            {
                return Session._Expect(new RegexMatch(query), handler, timeout.HasValue ? timeout.Value : this.Session.Timeout);
            }

        }

        public class SendCommands
        {
            private Session Session;

            internal SendCommands(Session parent)
            {
                if (ReferenceEquals(parent, null)) throw new ArgumentNullException("parent");
                this.Session = parent;
            }

            public void Char (char c, bool append_lt = false)
            { 
                Session._spawnable.Write(new string(c, 1) + (append_lt ? Session.LineTerminator : ""));
            }

            public void String(string s, bool append_lt = true)
            {
                Session._spawnable.Write(s + (append_lt ? Session.LineTerminator : ""));
            }

            public bool Command(string command, out string output, int? timeout = null)
            {
                output = string.Empty;
                StringBuilder cmd_builder = new StringBuilder(1000);
                cmd_builder.Append("echo \"<CMD_START>\" ;");
                cmd_builder.Append(command);
                cmd_builder.Append(";echo \"<CMD_END>\"");
                Session._spawnable.Write(cmd_builder.ToString() + Session.LineTerminator);
                IResult result =  Session.Expect.Regex("<CMD_START>\\r\\n([\\s\\S]*)\\r\\n<CMD_END>\\r\\n", null, timeout.HasValue ? timeout.Value : this.Session.Timeout);
                if (result.IsMatch)
                {
                    Re.Match m = (Re.Match) result.Result;
                    output = m.Groups[1].Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
