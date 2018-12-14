function createComponentService(apiService, modalService) {
    var srv = this;

    srv.enable = function (component) {
        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", true);
    };

    srv.disable = function (component) {
        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", false);
    };

    srv.toggleIsEnabled = function (component) {
        if (component.getSetting("is_enabled", true) === true) {
            srv.disable(component);
        } else {
            srv.enable(component);
        }
    };

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

                if (srv.componentUpdatedCallback !== undefined) {
                    srv.componentUpdatedCallback(response["component"]);
                }
            });
    };

    return this;
}