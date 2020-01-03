using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.ConsoleDemo.Menu
{
    [AttributeUsage(validOn: AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class CustomerAttribute : Attribute
    {
    }
}
