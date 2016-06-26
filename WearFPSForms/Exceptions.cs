using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WearFPSForms
{
    [Serializable]
    public class InitializeException : Exception
    {
        public InitializeException()
        {
        }

        public InitializeException(string message)
        : base(message)
        {
        }

        public InitializeException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }
}
