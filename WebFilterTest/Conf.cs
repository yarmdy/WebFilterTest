using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebFilterTest
{
    public static class Conf
    {
        public static string Domain { get { return ConfigurationManager.AppSettings["domain"] + ""; } }
        public static string CookieName { get { return ConfigurationManager.AppSettings["cookiename"] + ""; } }
        public static string CookieValue { get { return ConfigurationManager.AppSettings["cookievalue"] + ""; } }

        public static string ReplaceDomain(this string url) {
            var reg = new Regex(@"^(https{0,1}\:\/\/).+?(\/.*?|)$",RegexOptions.IgnoreCase|RegexOptions.Singleline);
            var match = reg.Match(url);
            if (match.Groups.Count<3) { return url; }
            return $"{match.Groups[1]}{Domain}{match.Groups[2]}";
        }
    }
}
