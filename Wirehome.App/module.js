var app;

(function () {
    app = angular.module("app", []);
    app.factory("localizationService", ["$http", createLocalizationService]);
    app.factory("apiService", ["$http", createApiService]);
    app.factory("modalService", [createModalService]);
    app.factory("componentService", ["apiService", "modalService", createComponentService]);
    app.factory("macroService", ["apiService", "modalService", createMacroService]);
    app.factory("notificationService", ["apiService", createNotificationService]);

    app.controller(
        "AppController",
        [
            "$http",
            "$scope",
            "apiService",
            "localizationService",
            "componentService",
            "macroService",
            "notificationService",
            createAppController
        ]);

})();