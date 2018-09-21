using CodeGen.AKS.Models;
using CodeGen.AKS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Kubernetes.Backend.API.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class AKSController : ControllerBase
    {
        private readonly KubeWrapper m_kube;
        private readonly ILogger<AKSController> m_log;

        public AKSController(KubeWrapper wrapper, ILogger<AKSController> log)
        {
            m_kube = wrapper;
            m_log = log;
        }

        [HttpGet("Deployments")]
        public async Task<IActionResult> Deployments()
        {
            var deps = await m_kube.GetDeployments();
            return new JsonResult(deps);
        }

        [HttpGet("Pods/{ns}")]
        public async Task<IActionResult> Pods(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return BadRequest("Namespace parameter is mandatory");

            var deps = await m_kube.GetPods(ns);
            return new JsonResult(deps);
        }

        [HttpPost("Pod")]
        public async Task<IActionResult> Pod(PodDeploymentModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Model was not properly fed");

            var podDeployment = await m_kube.CreatePodDeploymentInNamespace(model);
            return new JsonResult(podDeployment);
        }

        [HttpGet("Log/{pod}/{ns}")]
        public async Task<IActionResult> Log(string pod, string ns)
        {
            if (!ModelState.IsValid)
                return BadRequest("Model was not properly fed");

            var logs = await m_kube.GetPodLogs(pod, ns);
            return new JsonResult(logs);
        }

        [HttpGet("Metrics")]
        public async Task<IActionResult> Metrics()
        {
            var metrics = await m_kube.GetMetricsAsync();
            return new JsonResult(metrics);
        }

        [HttpGet("ExternalMetrics")]
        public async Task<IActionResult> ExternalMetrics()
        {
            var metrics = await m_kube.GetExternalMetricsAsync();
            return new JsonResult(metrics);
        }
    }
}
