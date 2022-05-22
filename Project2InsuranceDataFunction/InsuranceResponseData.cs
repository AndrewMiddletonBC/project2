using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Code by Andrew
 */

namespace Project2InsuranceDataFunction
{
    public class InsuranceResponseData
    {
        public string? PatientId { get; set; }
        public bool? HasInsurance { get; set; }

        public PolicyData? PolicyData { get; set; }
    }

    public class PolicyData
    {
        public string? PolicyNumber { get; set; }
        public string? Provider { get; set; }
    }
}
