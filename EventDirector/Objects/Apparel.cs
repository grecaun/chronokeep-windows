using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class Apparel
    {
        private int eventspecific_id;
        private string name, value;

        public int EventspecificId { get => eventspecific_id; set => eventspecific_id = value; }
        public string Name { get => name; set => name = value; }
        public string Value { get => value; set => this.value = value; }
    }
}
