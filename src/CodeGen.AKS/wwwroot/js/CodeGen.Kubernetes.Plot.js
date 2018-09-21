if (typeof (CodeGen) === "undefined") { CodeGen = { __namespace: true }; }
if (typeof (CodeGen.Kubernetes) === "undefined") { CodeGen.Kubernetes = { __namespace: true }; }

CodeGen.Kubernetes.Plot = new function () {
    var _self = this;

    _self.avg = [];
    _self.date = [];

    _self.clusterSettings = {
        metricsUrl: '/api/Kube/ExternalMetrics'
    };

    _self.onLoad = function () {
        var chartDiv = document.getElementById("myChart").getContext('2d');
        _self.Chart = new Chart(chartDiv, {
            type: 'line',
            data: {
                labels: _self.date,
                datasets: [{
                    label: 'CPU % utilization',
                    data: _self.avg,
                    backgroundColor: [
                        'rgba(54, 162, 235,0.2)',
                    ],
                    borderColor: [
                        'rgba(54, 162, 235, 1)',
                    ],
                    borderWidth: 3
                }]
            },
            options: {
                animation: {
                    duration: 0
                },
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }]
                }
            }
        });

        setInterval(_self.plot, 30000);
    };

    _self.plot = function () {
        $.ajax({
            url: _self.clusterSettings.metricsUrl,
            method: 'GET',
            success: function (dataFromJson) {
                _self.avg.push(dataFromJson.averageClusterCpu);

                var newDateConverted = new Date(dataFromJson.items[0].timestamp);
                var timeNow = newDateConverted.toLocaleTimeString();
                _self.date.push(timeNow);

                var lastItemsInDate = _self.date.slice(-60);
                var lastItemsInAverage = _self.avg.slice(-60);

                _self.Chart.update();
            }
        });
    };
}