using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.ConsoleDemo.Model
{
    public class PropType
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Ext { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }

    }
}
