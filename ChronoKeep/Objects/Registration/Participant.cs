using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects.Registration
{
    public class Participant
    {
        public string Bib { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Birthdate { get; set; }
        public string Gender { get; set; }
        public string Distance { get; set; }
        public string Mobile { get; set; }
        public bool TextEnabled { get; set; }
    }
}
