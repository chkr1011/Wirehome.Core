function createLocalizationService($http) {
    var srv = this;

    srv.uiLocalizations = {};
    srv.isInitialized = false;

    srv.get = function (key) {
        var result = srv.uiLocalizations[key];

        if (result === undefined) {
            result = "#" + key;
        }

        return result;
    };

    srv.load = function (language) {
        console.log("Loading localization '" + language + "'");

        $http.get("localizations/" + language + ".json").then(function (response) {
            if (response.status !== 200) {
                alert("Error while loading localization '" + language + "' (" + response.status + ").");
            } else {
                srv.uiLocalizations = response.data;
                srv.isInitialized = true;
            }
        });
    };

    return this;
}