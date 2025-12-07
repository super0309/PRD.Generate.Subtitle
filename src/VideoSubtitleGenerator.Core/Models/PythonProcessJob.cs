using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoSubtitleGenerator.Core.Models
{
    //internal class PythonProcessJob
    //{
    //}

    public class PythonProcessJob
    {
        public string phase { get; set; }
        public int percent { get; set; }
        public string message { get; set; }
        public int job_id { get; set; }
    }


}
