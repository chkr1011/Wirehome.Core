function createComponentService(apiService, modalService) {
    var srv = this;

    srv.sendMessage = function (componentUid, message) {
        apiService.executePost(
            "/api/v1/components/" + componentUid + "/process_message",
            message,
            function (response) {
                if (response["type"] !== "success") {
                    delete response["component"];
                    modalService.show("Processing component message failed!", JSON.stringify(response));

                    return;
                }

                if (response["message"] !== undefined) {
                    modalService.show("Command executed", response["message"]);
                }

                if (srv.componentUpdatedCallback !== undefined) {
                    srv.componentUpdatedCallback(response["component"]);
                }
            });
    };

    return this;
}