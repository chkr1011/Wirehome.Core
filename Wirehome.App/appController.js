function createAppController($http, $scope, modalService, apiService, localizationService, componentService, notificationService) {
    var c = this;

    $scope.getNumbers = function (num) {
        var a = new Array(num + 1);
        for (var i = 0; i < num + 1; i++) {
            a[i] = i;
        }

        return a;
    }

    $scope.getStateCaption = function (component, id) {
        if (component == undefined) {
            return id;
        }

        var caption = component.settings["app.caption.states." + id];
        if (caption !== undefined) {
            return caption;
        }

        return id;
    }

    $scope.selectedColorChanged = function (component) {
        if (component.appliedColor == undefined || component.appliedColor == component.selectedColor) {
            return;
        }

        c.componentService.setColor(component, component.selectedColor);
        component.appliedColor = component.selectedColor;
    }

    c.version = "-";
    c.status = {};
    c.componentGroups = [];
    c.notifications = [];
    c.weatherStation = {}

    c.notificationService = notificationService;
    c.componentService = componentService;
    c.localizationService = localizationService;
    c.apiService = apiService;

    c.showInfoPopover = function () {
        $("#infoIcon").popover({
            html: true,
            title: "Wirehome.App",
            placement: "top",
            content: function () {
                return $('#infoPopoverContent').html();
            }
        });
    }

    c.setActivePanel = function (id) {
        if (c.activePanel === id) {
            c.activePanel = "";
        } else {
            c.activePanel = id;
        }

        setTimeout(function () {
            $("html, body").animate({
                scrollTop: $("#" + id).offset().top
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
        console.log("Updating UI");
        var isFirstRun = false;

        if (c.isConfigured !== true) {
            isFirstRun = true;

            localizationService.load(status.global_variables["system.language_code"]);

            $.each(status.componentGroups, function (i, componentGroup) {
                if (!componentGroup.settings["app.is_visible"] === false) {
                    return;
                }

                componentGroupModel = {
                    uid: componentGroup.uid,
                    components: []
                };

                $.each(componentGroup.components, function (i, componentAssociation) {
                    componentModel = {
                        uid: i,
                        settings: componentAssociation.settings
                    };

                    componentGroupModel.components.push(componentModel);
                });

                c.componentGroups.push(componentGroupModel);
            });

            if (c.componentGroups.length === 1) {
                c.setActivePanel(c.componentGroups[0].uid);
            }

            c.isConfigured = true;
        }

        $.each(c.componentGroups, (i, componentGroupModel) => {
            var updatedComponentGroup = status.componentGroups.find(g => g.uid === componentGroupModel.uid);
            if (updatedComponentGroup === undefined) {
                return;
            }

            c.configureComponentGroup(componentGroupModel, updatedComponentGroup, status);
        });

        c.notifications = status.notifications;

        c.weatherStation.sunrise = status.global_variables["outdoor.sunrise"];
        c.weatherStation.sunset = status.global_variables["outdoor.sunset"];
        c.weatherStation.temperature = status.global_variables["outdoor.temperature"];
        c.weatherStation.humidity = status.global_variables["outdoor.humidity"];
        c.weatherStation.condition = status.global_variables["outdoor.condition"];
        c.weatherStation.conditionImage = status.global_variables["outdoor.condition.image_url"];

        c.isInitialized = true;
    };

    c.toggleIsEnabled = function (component) {
        if (component.settings["is_enabled"] === true) {
            componentService.disable(component);
        } else {
            componentService.enable(component);
        }
    };

    c.configureComponent = function (model, source, componentGroupModel) {
        model.status = source.status;
        model.settings = source.settings;
        model.configuration = source.configuration;
        model.source = source;

        var associationSettings = componentGroupModel.source.components[model.uid].settings;

        model.powerConsumption = getValue(source.status, "power.consumption", 0)

        model.caption = getEffectiveValue([associationSettings, source.settings], "app.caption", "#" + model.uid)
        model.sortValue = getEffectiveValue([associationSettings, source.settings], "app.position_index", 0);
        model.imageColor = getEffectiveValue([associationSettings, source.settings], "app.image_color", "#427AB6");

        if (model.template == undefined) {
            if (source.status["motion_detection.state"] !== undefined) {
                model.template = "views/components/motionDetectorTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-walking");
            }
            else if (source.status["button.state"] !== undefined) {
                model.template = "views/components/buttonTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-hand-point-right");
            }
            else if (source.status["temperature.value"] !== undefined) {
                model.template = "views/components/temperatureSensorTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-thermometer-half");
            }
            else if (source.status["humidity.value"] !== undefined) {
                model.template = "views/components/humiditySensorTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-tint");

                model.dangerValue = getValue(source.settings, "app.humidity.danger_value", 75);
                model.warningValue = getValue(source.settings, "app.humidity.warning_value", 60);
            }
            else if (source.status["roller_shutter.state"] !== undefined) {
                model.template = "views/components/rollerShutterTemplate.html";
            }
            else if (source.status["state_machine.state"] !== undefined) {
                model.template = "views/components/stateMachineTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-check-double");
            }
            else if (source.status["level.current"] !== undefined) {
                model.template = "views/components/ventilationTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-wind");
            }
            else if (source.status["color.red"] !== undefined) {
                model.template = "views/components/rgbTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-palette");

                model.selectedColor = "#FFFFFF";
            }
            else if (source.status["display.text"] !== undefined) {
                model.template = "views/components/displayTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-desktop");
            }
            else if (source.status["power.state"] !== undefined) {
                model.template = "views/components/toggleTemplate.html";
                model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-power-off");
            }
        }

        if (model.imageId === undefined) {
            model.imageId = getEffectiveValue([associationSettings, source.settings], "app.image_id", "fas fa-square");
        }

        if (model.template == "views/rgbTemplate.html") {
            var r = source.status["color.red"];
            var g = source.status["color.green"];
            var b = source.status["color.blue"];
            var hex = c.componentService.rgbToHex(r, g, b);
            model.appliedColor = hex;
            model.selectedColor = hex;
        }
    }

    c.configureComponentGroup = function (model, source, status) {
        model.source = source;

        model.caption = getValue(source.settings, "app.caption", "#" + source.uid);
        model.sortValue = getValue(source.settings, "app.position_index", 0);
        model.isVisible = getValue(source.settings, "app.is_visible", true);
        model.imageId = getValue(source.settings, "app.image_id", null);
        model.imageColor = getValue(source.settings, "app.image_color", null);

        $.each(model.components, function (i, componentModel) {
            var componentStatus = status.components.find(x => x.uid == componentModel.uid);
            if (componentStatus === undefined) {
                return;
            }

            c.configureComponent(componentModel, componentStatus, model);
        });
    }

    $http.get("version.txt").then(function (response) {
        c.version = response.data;
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
    if (source === undefined) {
        return defaultValue;
    }

    var value = source[name];

    if (value === undefined || value === null) {
        return defaultValue;
    }

    return value;
}