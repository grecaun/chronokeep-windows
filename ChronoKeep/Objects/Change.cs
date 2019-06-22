using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class Change
    {
        int identifier;
        Participant newParticipant, oldParticipant;

        public Change() { }

        public Change(int identifier, Participant np, Participant op)
        {
            this.identifier = identifier;
            this.newParticipant = np;
            this.oldParticipant = op;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        internal Participant NewParticipant { get => newParticipant; set => newParticipant = value; }
        internal Participant OldParticipant { get => oldParticipant; set => oldParticipant = value; }
    }
}
