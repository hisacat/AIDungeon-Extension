using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace AIDungeon_Extension
{
    public static class MachineKeyProtect
    {
        public static string Protect(string text, string purpose)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                byte[] stream = Encoding.UTF8.GetBytes(text);
                byte[] encoded = MachineKey.Protect(stream, purpose);
                return HttpServerUtility.UrlTokenEncode(encoded);
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
        public static string Unprotect(string text, string purpose)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                byte[] stream = HttpServerUtility.UrlTokenDecode(text);
                byte[] decoded = MachineKey.Unprotect(stream, purpose);
                return Encoding.UTF8.GetString(decoded);
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
    }
}
