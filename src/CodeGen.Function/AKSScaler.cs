using Microsoft.Azure.Management.ContainerService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;

namespace CodeGen.Function
{
    public static class AKSScaler
    {
        [FunctionName("AKSScaler")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var tenantId = "1dc1085b-9aa9-42dc-bedd-4c7dcfe4570e";
            var clientSecret = "Code@Gen2018";
            var clientId = "c56cd20f-df34-4ab6-8c3d-bc90547d2838";
            var clusterId = "/subscriptions/f6fec764-fbe4-42a9-8f27-93cfe2f839cd/resourcegroups/CodeGen2018/providers/Microsoft.ContainerService/managedClusters/CodeGen";
            var url = "https://codegenaks-31b27eec.hcp.northeurope.azmk8s.io:443/api/v1/namespaces/kube-system/services/http:heapster:/proxy/apis/metrics/v1alpha1/nodes?labelSelector=";
            var token = "701c47b9b820885ffa01f2727829d57a";

            double averageUsage;
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true };
            using (var client = new HttpClient(handler))
            {
                log.LogInformation($"Querying Cluster Metrics...");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                var response = client.GetStringAsync(url).GetAwaiter().GetResult();
                var typedMetric = JsonConvert.DeserializeObject<K8sMetric>(response);
                averageUsage = Math.Round(typedMetric.Items.Average(x => x.Usage.CPUUsage), 0);
            }

            log.LogInformation($"Average CPU usage is {averageUsage}");
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Configure().Authenticate(credentials).WithDefaultSubscription();
            IKubernetesCluster kubernetesCluster = azure.KubernetesClusters.GetById(clusterId);

            var agentPool = kubernetesCluster.AgentPools.FirstOrDefault().Value as IKubernetesClusterAgentPool;
            var nodeCount = agentPool.Count;
            log.LogInformation($"Current node count is {nodeCount}");

            if (!kubernetesCluster.ProvisioningState.Equals("Succeeded"))
            {
                log.LogInformation($"Cluster level operation in progress. State: {kubernetesCluster.ProvisioningState}");
                return;
            }

            if (averageUsage >= 50)
            {
                nodeCount += 1;
                log.LogInformation($"Scaling up by 1 VM");
                kubernetesCluster.Update().WithAgentPoolVirtualMachineCount(nodeCount).Apply();
            }
            else if (averageUsage < 50 && nodeCount > 3)
            {
                nodeCount -= 1;
                log.LogInformation($"Scaling down by 1 VM");
                kubernetesCluster.Update().WithAgentPoolVirtualMachineCount(nodeCount).Apply();
            }

            log.LogInformation($"Autoscaling ended...");
        }
    }
}

public class K8sMetric
{
    public Metric[] Items { get; set; }
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