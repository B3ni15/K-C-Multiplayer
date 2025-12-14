using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM
{
    public static class ErrorCodeMessages
    {
        private static readonly Dictionary<string, string> errorMessages = new Dictionary<string, string>
        {
            { "TimedOut", "Your connectioned has timed out." },
            { "Disconnected", "Lost connection to server." },
            { "NoConnection", "Failed to connect to server." },
        };

        public static string GetMessage(Enum errorCode)
        {
            if (errorMessages.TryGetValue(errorCode.ToString(), out string message))
            {
                return message;
            }
            return errorCode.ToString(); // Fallback message
        }
    }
}
