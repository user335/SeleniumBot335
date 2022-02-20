using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User335
{
    public static class LogFunctions
    {
        public static void WriteInfo(string message)
        {
            message = "<div class\"infoMessage\"> " + message + " </div>";
            LogFunctions.WriteInfo(message);
        }
        public static void WriteError(string message)
        {
            message = "<div class\"errorMessage\"> " + message + " </div>";
            LogFunctions.WriteInfo(message);
        }
        public static void ThrowError(string message)
        {
            message = "<div class\"errorMessage\"> " + message + " </div>";
            throw new Exception(message);
        }
    }
}
