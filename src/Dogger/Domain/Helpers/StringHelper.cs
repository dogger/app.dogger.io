using System.Globalization;
using System.Text;

namespace Dogger.Domain.Helpers
{
    public static class StringHelper
    {
        public static string ToHexadecimal(string text)
        {
            return ToHexadecimal(
                Encoding.UTF8.GetBytes(text));
        }

        public static string ToHexadecimal(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var @byte in bytes)
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", @byte);

            return builder.ToString();
        }
    }
}
