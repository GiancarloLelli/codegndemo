using System;
using System.Linq;

namespace CodeGen.AKS.Models
{
    public class K8sMetric
    {
        public Metric[] Items { get; set; }

        public double AverageClusterCpu
        {
            get
            {
                var round = 0.0D;

                if (Items != null)
                    round = Math.Round(Items.Average(x => x.Usage.CPUUsage), 0);

                return round;
            }
        }
    }

    public class Metric
    {
        public DateTime Timestamp { get; set; }
        public Usage Usage { get; set; }
    }

    public class Usage
    {
        public string Cpu { get; set; }
        public string Memory { get; set; }
        public double CPUUsage
        {
            get
            {
                var strippedText = Cpu.Replace("m", string.Empty);
                var typedUsage = int.Parse(strippedText);
                double percentage = typedUsage / 10;
                return percentage;
            }
        }
    }
}
