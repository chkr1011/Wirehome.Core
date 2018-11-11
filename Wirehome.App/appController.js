function createAppController($http, $scope, apiService, localizationService, componentService, notificationService) {
    var c = this;

    extendAngularScope($scope);

    c.version = "-";
    c.notifications = [];
    c.componentGroups = [];
    c.globalVariables = {}

    c.notificationService = notificationService;
    c.componentService = componentService;
    c.localizationService = localizationService;
    c.apiService = apiService;

    c.hasGlobalVariable = function (uid) {
        c.globalVariables.hasOwnProperty(uid);
    };

    c.getGlobalVariable = function (uid, defaultValue) {
        return getValue(c.globalVariables, uid, defaultValue);
    };

    c.setActivePanel = function (uid) {
        if (c.activePanel === uid) {
            c.activePanel = "";
        } else {
            c.activePanel = uid;
        }

        setTimeout(function () {
            $("html, body").animate({
                scrollTop: $("#" + uid).offset().top
            }, 250);
        }, 100);
    }

    c.applyNewComponentStatus = function (updatedComponent) {
        $.each(c.componentGroups, function (i, componentGroupModel) {
            $.each(componentGroupModel.components, function (j, componentModel) {
                if (componentModel.uid == updatedComponent.uid) {
                    c.configureComponent(componentModel, updatedComponent, componentGroupModel)
                }
            });
        });
    }

    c.applyNewStatus = function (status) {
        if (c.isConfigured !== true) {
            console.log("Building UI...");
            localizationService.load(status.global_variables["system.language_code"]);

            var urlParams = new URLSearchParams(window.location.search);
            var selectedComponentGroup = urlParams.get('componentGroup');

            $.each(status.componentGroups, function (i, componentGroup) {
                if (componentGroup.settings["app.is_visible"] === false) {
                    return;
                }

                if (selectedComponentGroup != null && componentGroup.uid != selectedComponentGroup) {
                    return;
                }

                var componentGroupModel = {
                    uid: componentGroup.uid,
                    components: []
                };

                componentGroupModel.getSetting = function (uid, defaultValue) {
                    return getValue(componentGroupModel.source.settings, uid, defaultValue);
                };

                componentGroupModel.hasSetting = function (uid) {
                    return componentGroupModel.source.settings.hasOwnProperty(uid);
                }

                $.each(componentGroup.components, function (i) {
                    var componentModel = {
                        uid: i,
                        source: {}
                    };

                    componentModel.hasStatus = function (uid) {
                        return componentModel.source.status.hasOwnProperty(uid);
                    }

                    componentModel.getStatus = function (uid, defaultValue) {
                        return getValue(componentModel.source.status, uid, defaultValue);
                    }

                    componentModel.hasSetting = function (uid) {
                        var associationSettings = componentGroupModel.source.components[componentModel.uid].settings;
                        return associationSettings.hasOwnProperty(uid) || componentModel.source.settings.hasOwnProperty(uid);
                    }

                    componentModel.getSetting = function (uid, defaultValue) {
                        var associationSettings = componentGroupModel.source.components[componentModel.uid].settings;
                        return getEffectiveValue([associationSettings, componentModel.source.settings], uid, defaultValue);
                    }

                    componentModel.hasConfiguration = function (uid) {
                        return componentModel.source.configuration.hasOwnProperty(uid);
                    }

                    componentModel.getConfiguration = function (uid, defaultValue) {
                        return getValue(componentModel.source.configuration, uid, defaultValue);
                    }

                    componentGroupModel.components.push(componentModel);
                });

                c.componentGroups.push(componentGroupModel);
            });

            if (c.componentGroups.length === 1) {
                c.setActivePanel(c.componentGroups[0].uid);
            }

            c.isConfigured = true;
        }

        console.log("Updating UI...");
        $.each(c.componentGroups, (i, componentGroupModel) => {
            var updatedComponentGroup = status.componentGroups.find(g => g.uid === componentGroupModel.uid);
            if (updatedComponentGroup === undefined) {
                return;
            }

            c.configureComponentGroup(componentGroupModel, updatedComponentGroup, status);
        });

        c.notifications = status.notifications;
        c.globalVariables = status.global_variables;

        c.isInitialized = true;
    };

    c.configureComponent = function (model, source, componentGroupModel) {
        model.source = source;

        model.template = getValue(source.configuration, "app.view_source", null);
        if (model.template == undefined || model.template == null) {
            model.template = "views/componentViewMissing.html";
        }

        var associationSettings = componentGroupModel.source.components[model.uid].settings;
        model.sortValue = getEffectiveValue([associationSettings, source.settings], "app.position_index", model.uid);
    }

    c.configureComponentGroup = function (model, source, status) {
        model.source = source;

        model.sortValue = getValue(source.settings, "app.position_index", model.uid);

        $.each(model.components, function (i, componentModel) {
            var componentStatus = status.components.find(x => x.uid == componentModel.uid);
            if (componentStatus === undefined) {
                return;
            }

            c.configureComponent(componentModel, componentStatus, model);
        });
    }

    $http.get("app.manifest").then(function (response) {
        var parser = new RegExp("# version: ([0-9|.]*)", "");
        var results = parser.exec(response.data);

        c.version = results[1];
    });

    componentService.componentUpdatedCallback = c.applyNewComponentStatus;

    // Start the polling loop for new status.
    apiService.newStatusReceivedCallback = c.applyNewStatus;
    apiService.pollStatus();
}

function getEffectiveValue(sourceList, name, defaultValue) {
    var value = null;
    $.each(sourceList, (i, source) => {
        value = getValue(source, name, null);
        if (value != null) {
            return false; // Break the loop.
        }
    });

    if (value != null) {
        return value;
    }

    return defaultValue;
}

function getValue(source, name, defaultValue) {
    if (source === undefined || source == null) {
        return defaultValue;
    }

    if (!source.hasOwnProperty(name)) {
        return defaultValue;
    }

    return source[name];
}