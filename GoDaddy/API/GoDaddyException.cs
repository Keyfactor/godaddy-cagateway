using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.API
{
    class GoDaddyException : ApplicationException
    {
        public GoDaddyException(string message) : base(message)
        { }

        public GoDaddyException(string message, Exception ex) : base(message, ex)
        { }
        public static string FlattenExceptionMessages(Exception ex, string message)
        {
            message += ex.Message + Environment.NewLine;
            if (ex.InnerException != null)
                message = FlattenExceptionMessages(ex.InnerException, message);

            return message;
        }
    }
}
