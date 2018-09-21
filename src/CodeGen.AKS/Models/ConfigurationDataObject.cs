namespace CodeGen.AKS.Models
{
    public class ConfigurationDataObject
    {
        public string Host { get; set; }
        public string Token { get; set; }
        public string ClientCertificateData { get; set; }
        public string ClientCertificateKeyData { get; set; }
        public string CA { get; set; }
    }
}
