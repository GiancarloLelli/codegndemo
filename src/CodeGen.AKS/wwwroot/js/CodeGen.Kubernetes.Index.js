if (typeof (CodeGen) === "undefined") { CodeGen = { __namespace: true }; }
if (typeof (CodeGen.Kubernetes) === "undefined") { CodeGen.Kubernetes = { __namespace: true }; }

CodeGen.Kubernetes.Index = new function () {
    var _self = this;

    _self.clusterSettings = {
        deployUrl: '/api/AKS/Pod'
    };

    _self.onLoad = function () {
        $('#deployButton').click(function () { _self.deployContainer(); });
        CodeGen.Kubernetes.Plot.onLoad();
    };

    _self.deployContainer = function () {
        $('#deployButton').prop('disabled', true);

        if (!$('#okMessage').hasClass('d-none')) {
            $('#okMessage').addClass('d-none');
        }

        var payload = {
            image: $('#imageName').val(),
            name: $('#podName').val(),
            cmd: $('#command').val(),
            ns: $('#ns').val(),
            args: $('#cmd').val()
            //,count: $('#count').val()
        };

        $.ajax({
            type: 'POST',
            url: _self.clusterSettings.deployUrl,
            data: JSON.stringify(payload),
            success: _self.deployCallback,
            error: _self.errorCallback,
            contentType: 'application/json; charset=utf-8',
            dataType: 'json'
        });
    };

    _self.deployCallback = function (result) {
        if (!$('#koMessage').hasClass('d-none')) {
            $('#koMessage').addClass('d-none');
        }

        $('#okMessage').removeClass('d-none');
        $('#deployButton').prop('disabled', false);

        var response = result.Pod;
        _self.clusterSettings.pod = response;
    };

    _self.errorCallback = function (xhr, error) {
        var text = xhr.responseText;
        $('#errorMessage').val(text);
        $('#koMessage').removeClass('d-none');
        $('#deployButton').prop('disabled', false);
    };
}