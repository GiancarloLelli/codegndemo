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
            var clientSecret = "Open@Shift1";
            var token = "80b9f59be248ffbb1bce97201c1c705d";
            var clientId = "31ddab0f-8377-48c8-b996-87e38bc00846";
            var tenantId = "5260bcfe-56e3-4379-8d89-b18cd76213aa";
            var clusterId = "/subscriptions/8f03dbc7-726d-4567-9479-925323a02f45/resourcegroups/LcRsrGrp/providers/Microsoft.ContainerService/managedClusters/acic-k8s";
            var url = "https://acic-k8s-92d90c3d.hcp.westeurope.azmk8s.io:443/api/v1/namespaces/kube-system/services/http:heapster:/proxy/apis/metrics/v1alpha1/nodes?labelSelector=";

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