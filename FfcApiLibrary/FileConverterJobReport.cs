using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace idox.eim.fusionp8
{
    public class FileConverterJobReport
    {
        public string Id { get; set; }
        public string ConversionStatus { get; set; }
        public int ErrorCode { get; set; } = 0;
        public string ErrorDescription { get; set; }
        public string TimeStamp { get; set; }
        public int Submitted { get; set; }
        public DateTime Completed { get; set; }
        public double ConversionTime { get; set; }
        public string MsmqRecord { get; set; }
        public string MsmqPath { get; set; }
    }
}
