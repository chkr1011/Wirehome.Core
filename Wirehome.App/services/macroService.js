function createMacroService(apiService, modalService) {
    var srv = this;

    srv.execute = function (macro, message) {
        apiService.executePost("/api/v1/macros/" + macro.uid + "/execute",
            message,
            function (response) {
                if (response["type"] !== "success") {
                    modalService.show("Executing macro failed!", JSON.stringify(response));
                    return;
                }
            });
    };

    return this;
}