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
            string tmpHeadLine = $"<h1>{line}</h1>";
            if (consoleColor != ConsoleColor.Black)
                tmpHeadLine = $"<font color=\"{consoleColor.ToString()}\">{tmpHeadLine}</h1>";

            _Html.AppendLine(tmpHeadLine);
        }

        public void Add(string text, ConsoleColor consoleColor = ConsoleColor.Black,bool AddNewLine = false)
        {
            var htmlToAdd = new StringBuilder();

            if (consoleColor == ConsoleColor.Black)
                htmlToAdd.Append(text);
            else
                htmlToAdd.Append($"<font color=\"{consoleColor.ToString()}\">{text}</font>");

            if(AddNewLine)
            {
                htmlToAdd.AppendLine("</br>");
            }

            _Html.Append(htmlToAdd);
        }

        public void AddLine(string line,ConsoleColor consoleColor = ConsoleColor.Black)
        {
            Add(line, consoleColor, true);
        }

        public override string ToString()
        {
            return "<html xmlns=\"https://www.w3.org/1999/xhtml/\">  <body> " + _Html.ToString() + "</body></html> ";
        }
    }
}
