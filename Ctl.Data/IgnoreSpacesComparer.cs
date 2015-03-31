using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ctl.Data
{
    /// <summary>
    /// Compares 
    /// </summary>
    public class IgnoreSpacesComparer : StringComparer
    {
        static readonly StringComparer defaultInstance = new IgnoreSpacesComparer();

        static StringComparer Default
        {
            get { return defaultInstance; }
        }

        readonly StringComparer baseComparer;

        public IgnoreSpacesComparer()
        {
            this.baseComparer = StringComparer.OrdinalIgnoreCase;
        }

        public IgnoreSpacesComparer(StringComparer baseComparer)
        {
            this.baseComparer = baseComparer ?? StringComparer.OrdinalIgnoreCase;
        }

        public override int Compare(string x, string y)
        {
            return baseComparer.Compare(StripSpaces(x), StripSpaces(y));
        }

        public override bool Equals(string x, string y)
        {
            return baseComparer.Equals(StripSpaces(x), StripSpaces(y));
        }

        public override int GetHashCode(string obj)
        {
            return baseComparer.GetHashCode(StripSpaces(obj));
        }

        static string StripSpaces(string s)
        {
            if (s == null || !s.Any(ch => char.IsWhiteSpace(ch)))
            {
                return s;
            }

            StringBuilder sb = new StringBuilder();

            int charLen;

            for (int i = 0; i < s.Length; i += charLen)
            {
                charLen = char.IsSurrogatePair(s, i) ? 2 : 1;

                if (!char.IsWhiteSpace(s, i))
                {
                    sb.Append(s, i, charLen);
                }
            }

            return sb.ToString();
        }
    }
}
