using System;
using System.Security.Cryptography;
using System.Text;

namespace TNRD.ZeepLogicScript.Spike
{
    /// <summary>
    /// The Script text stored on the carrier block, plus a long test text with characters that must survive
    /// JSON escaping (quotes, backslashes, tabs, non-ASCII).
    /// </summary>
    internal static class SpikeCarrier
    {
        public const int LanguageVersion = 1;
        public const string TestName = "spike \"carrier\" test";

        // Spike-only format: version, scriptId, name, then the text, separated by the first three newlines
        public static string Serialize(string scriptId, string name, string text)
        {
            return LanguageVersion + "\n" + scriptId + "\n" + name + "\n" + text;
        }

        public static bool TryParse(string value, out int version, out string scriptId, out string name, out string text)
        {
            version = 0;
            scriptId = name = text = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string[] parts = value.Split(['\n'], 4);
            if (parts.Length < 4 || !int.TryParse(parts[0], out version))
                return false;

            scriptId = parts[1];
            name = parts[2];
            text = parts[3];
            return true;
        }

        public static string BuildTestText()
        {
            StringBuilder builder = new();
            builder.Append("# carrier test: quotes \" ' backslash \\ tab\t unicode é ü ✓ 漢字 🚗\n");
            for (int i = 0; i < 200; i++)
            {
                builder.Append("line_").Append(i).Append(" = placed(\"aB3kQ9xLmP2wR7t\") & \"x\\y\" # ")
                    .Append(i % 2 == 0 ? "even" : "odd").Append('\n');
            }

            builder.Append("# end");
            return builder.ToString();
        }

        public static string Sha256(string text)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
