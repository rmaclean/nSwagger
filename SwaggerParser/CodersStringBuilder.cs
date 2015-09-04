
namespace SwaggerParser
{
    using System.Text;

    internal class CodersStringBuilder
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();
        private int indentLevel = 0;

        public void Indent()
        {
            indentLevel++;
        }

        public void Outdent()
        {
            indentLevel--;
        }

        public void AppendLine(string code)
        {

            if (code == "}")
            {
                Outdent();
            }

            var spacing = "".PadRight(indentLevel * 4, ' ');
            stringBuilder.AppendLine(spacing + code);
            if (code == "{")
            {
                Indent();
            }
        }

        public void AppendLine()
        {
            stringBuilder.AppendLine();
        }

        public void Append(string code, bool includePadding = true)
        {
            var spacing = "";
            if (includePadding)
            {
                spacing = "".PadRight(indentLevel * 4, ' ');
            }

            stringBuilder.Append(spacing + code);
        }

        public override string ToString() => stringBuilder.ToString();
    }
}