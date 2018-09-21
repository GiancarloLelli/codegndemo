using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CodeGen.AKS.Models
{
    [DataContract]
    public class PodDeploymentModel
    {
        [Required, DataMember(Name = "image")]
        public string Image { get; set; }

        [Required, DataMember(Name = "name")]
        public string Name { get; set; }

        [Required, DataMember(Name = "cmd")]
        public string Command { get; set; }

        [Required, DataMember(Name = "args")]
        public string Arguments { get; set; }

        [Required, DataMember(Name = "ns")]
        public string Namespace { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
}
