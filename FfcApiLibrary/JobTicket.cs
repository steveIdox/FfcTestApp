using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idox.eim.fusionp8
{
    public class JobTicket
    {
        public List<string> InputFiles { get; set; }
        public string OutputFilePath { get; set; }
        public List<JobDefinition> Jobs { get; set; }
        // ... etc ...
    }
}
