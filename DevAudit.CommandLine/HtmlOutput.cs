using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.CommandLine
{
    class HtmlOutput
    {
        StringBuilder _Html;

        public HtmlOutput()
        {
            _Html = new StringBuilder();
        }

        public void AddParagraph()
        {
            _Html.AppendLine("<p>");
        }

        public void EndParagraph()
        {
            _Html.AppendLine("</p>");
        }

        public void AddHeadLine(string line, ConsoleColor consoleColor = ConsoleColor.Black)
        {
            if (consoleColor == ConsoleColor.Black)
                _Html.AppendLine(line);
            else
                _Html.AppendLine($"<font color=\"{consoleColor.ToString()}\"><h1>{line}<h2></font></br>");
        }

        public void AddLine(string line,ConsoleColor consoleColor = ConsoleColor.Black)
        {
            if (consoleColor == ConsoleColor.Black)
                _Html.AppendLine(line);
            else
                _Html.AppendLine($"<font color=\"{consoleColor.ToString()}\">{line}</font> </br>");
        }

        public override string ToString()
        {
            return _Html.ToString();
        }
    }
}
