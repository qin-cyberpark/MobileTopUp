using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MobileTopUp
{
    public class UnknownDataException : Exception
    {
        public UnknownDataException(string dataField, string invalidValue) : base(dataField+ ":" + invalidValue)
        {
        }
    }
}