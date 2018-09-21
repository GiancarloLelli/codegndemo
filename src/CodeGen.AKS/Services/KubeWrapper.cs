using CodeGen.AKS.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeGen.AKS.Services
{
    public class KubeWrapper
    {
        private readonly ILogger<KubeWrapper> m_logger;
        private readonly KubernetesClientConfiguration m_configuration;
        private readonly IKubernetes m_service;

        public KubeWrapper(ILogger<KubeWrapper> logger, IOptions<ConfigurationDataObject> config)
        {
            m_logger = logger;

            var concreteConfig = config.Value;
            m_configuration = new KubernetesClientConfiguration
            {
                Host = concreteConfig.Host,
                AccessToken = concreteConfig.Token,
                ClientCertificateData = concreteConfig.ClientCertificateData,
                ClientCertificateKeyData = concreteConfig.ClientCertificateKeyData,
                SkipTlsVerify = true
            };

            m_service = new k8s.Kubernetes(m_configuration);
        }

        public async Task<dynamic> GetDeployments()
        {
            dynamic deployments = new ExpandoObject();

            try
            {
                var k8sDeployments = await m_service.ListDeploymentForAllNamespaces3Async();
                deployments.ApiVersion = k8sDeployments.ApiVersion;
                deployments.Deployments = k8sDeployments.Items;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
            }

            return deployments;
        }

        public async Task<dynamic> CreatePodDeploymentInNamespace(PodDeploymentModel podModel)
        {
            dynamic pod = new ExpandoObject();
            var createdPods = new List<V1Pod>();
            var count = podModel.Count == 0 ? 1 : podModel.Count;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var podConfiguration = new V1Pod
                    {
                        ApiVersion = "v1",
                        Kind = "Pod",
                        Metadata = new V1ObjectMeta { Name = $"{podModel.Name}{i}" },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = podModel.Name,
                                    Image = podModel.Image,
                                    ImagePullPolicy = "Always",
                                    Command = new List<string> { podModel.Command },
                                    Args = new List<string> { podModel.Arguments },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity("0.3")},
                                            { "memory", new ResourceQuantity("10Mi")}
                                        },
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            { "cpu", new ResourceQuantity("0.9")},
                                            { "memory", new ResourceQuantity("15Mi")}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    var newPod = await m_service.CreateNamespacedPodAsync(podConfiguration, podModel.Namespace);
                    createdPods.Add(newPod);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
            }

            pod.Pods = createdPods;
            return pod;
        }

        public async Task<string> GetPodLogs(string pod, string ns)
        {
            string log = string.Empty;

            try
            {
                using (var k8sPods = await m_service.ReadNamespacedPodLogAsync(pod, ns))
                {
                    using (var reader = new StreamReader(k8sPods))
                    {
                        log = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
            }

            return log;
        }

        public async Task<dynamic> GetPods(string ns)
        {
            dynamic pods = new ExpandoObject();

            try
            {
                var k8sPods = await m_service.ListNamespacedPodAsync(ns);
                pods.ApiVersion = k8sPods.ApiVersion;
                pods.Pods = k8sPods.Items;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
            }

            return pods;
        }

        public async Task<dynamic> GetMetricsAsync()
        {
            dynamic metrics = new ExpandoObject();

            try
            {
                var k8sNodes = await m_service.ListNodeAsync();
                metrics.ApiVersion = k8sNodes.ApiVersion;
                metrics.Metrics = k8sNodes.Items.Select(x => new
                {
                    Name = x.Metadata.Name,
                    Allocatable = x.Status.Allocatable,
                    Capacity = x.Status.Capacity
                });
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
            }

            return metrics;
        }

        public async Task<K8sMetric> GetExternalMetricsAsync()
        {
            var url = $"{m_configuration.Host}/api/v1/namespaces/kube-system/services/http:heapster:/proxy/apis/metrics/v1alpha1/nodes?labelSelector=";
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, b, c, d) => true };
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {m_configuration.AccessToken}");
                var response = await client.GetStringAsync(url);
                var metric = JsonConvert.DeserializeObject<K8sMetric>(response);
                return metric;
            }
        }
    }
}
