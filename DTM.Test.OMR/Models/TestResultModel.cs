using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTM.Test.OMR.Models
{
    public class TestResultModel
    {

        public string FullName { get; set; }
        public long Number { get; set; }

        public DateTime Date { get; set; }
        public IDictionary<int, string> Resuls { get; set; }

        public TestResultModel()
        {
            Resuls = new Dictionary<int, string>();
            Date = DateTime.Now;
        }
    }
}
