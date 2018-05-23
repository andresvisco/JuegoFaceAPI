using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Coincidence
    {
        public Guid MatchId { get; set; }
        public Guid NewId { get; set; }
        public double Confidence { get; set; }
    }
}
