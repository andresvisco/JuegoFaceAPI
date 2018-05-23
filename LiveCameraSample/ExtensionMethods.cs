using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveCameraSample
{
    public static class ExtensionMethods
    {
        public static bool IsBetween(this int value, int start, int end)
        {
            return (start <= value && value <= end);
        }
    }
}
