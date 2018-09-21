function getVersion(callback) {
    $.get("cache.manifest", function (data) {
        var parser = new RegExp("# Version ([0-9|.]*)", "");
        var results = parser.exec(data);

        callback(results[1]);
    });
}

function createAppController($http, $scope, modalService, apiService, localizationService, componentService, notificationService) {
    var c = this;

    $scope.getVersion = function (version) {
        c.version = version;
    };

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

    c.appConfiguration = {
        showWeatherStation: true,
        showSensorsOverview: true,
        showRollerShuttersOverview: true,
        showMotionDetectorsOverview: true,
        showWindowsOverview: true
    }

    c.status = {};

    c.areas = [];

    c.notifications = [];
    c.weatherStation = {}

    c.sensors = [];
    c.rollerShutters = [];
    c.motionDetectors = [];
    c.windows = [];

    c.version = "-";

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

    c.showSetColorPopover = function (component) {
        if (component.State.ColorState == undefined) {
            return;
        }

        if (component.showColorSelector === true) {
            component.showColorSelector = false;
            return;
        }

        component.colorSelector.hue = component.State.ColorState.Hue;
        component.colorSelector.saturation = component.State.ColorState.Saturation;
        component.colorSelector.value = component.State.ColorState.Value;

        component.showColorSelector = true;
    }

    c.hideSetColorPopover = function (component) {
        component.showColorSelector = false;
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

    c.applyNewStatus = function (status) {
        console.log("Updating UI");
        var isFirstRun = false;

        if (c.isConfigured !== true) {
            isFirstRun = true;

            localizationService.load(status.global_variables["system.language_code"]);

            $.each(status.areas, function (i, area) {
                if (!area.settings["is_enabled"] === false) {
                    return;
                }

                areaModel = {
                    uid: area.uid,
                    components: []
                };

                $.each(area.components, function (i, componentUid) {
                    componentModel = {
                        uid: componentUid
                    };

                    areaModel.components.push(componentModel);
                });

                c.areas.push(areaModel);
            });

            if (c.areas.length === 1) {
                c.setActivePanel(c.areas[0].uid);
            }

            c.isConfigured = true;
        }

        $.each(c.areas, (i, areaModel) => {
            var updatedArea = status.areas.find(a => a.uid === areaModel.uid);
            if (updatedArea === undefined) {
                return;
            }

            c.configureArea(areaModel, updatedArea, status, isFirstRun);
        });

        c.appConfiguration.showSensorsOverview = c.sensors.length > 0;
        c.appConfiguration.showRollerShuttersOverview = c.rollerShutters.length > 0;
        c.appConfiguration.showMotionDetectorsOverview = c.motionDetectors.length > 0;
        c.appConfiguration.showWindowsOverview = c.windows.length > 0;

        c.notifications = status.notifications;

        c.weatherStation.sunrise = status.global_variables["outdoor.sunrise"];
        c.weatherStation.sunset = status.global_variables["outdoor.sunset"];
        c.weatherStation.temperature = status.global_variables["outdoor.temperature"];
        c.weatherStation.humidity = status.global_variables["outdoor.humidity"];
        c.weatherStation.condition = status.global_variables["outdoor.condition"];
        c.weatherStation.conditionImage = status.global_variables["outdoor.condition.image_url"];

        c.isInitialized = true;
    };

    c.updateComponentState = function (componentId, updatedComponent) {
        $.each(c.areas, function (i, area) {
            $.each(area.components, function (j, component) {
                if (component.Id === componentId) {
                    component.Settings = updatedComponent.Settings;
                    component.State = updatedComponent.State;

                    if (component.onStateChangedCallback != undefined) {
                        component.onStateChangedCallback(component);
                    }
                }
            });
        });
    };

    c.toggleIsEnabled = function (component) {
        if (component.settings["is_enabled"] === true) {
            componentService.disable(component);
        } else {
            componentService.enable(component);
        }
    };

    c.configureComponent = function (component, area, source, isFirstRun) {
        component.status = source.status;
        component.settings = source.settings;
        component.configuration = source.configuration;

        component.caption = getSetting(component.settings, "app.caption", "#" + component.uid)
        component.overviewCaption = getSetting(component.settings, "app.overview_caption", area.Caption + " / " + component.Caption)
        component.image = getSetting(component.settings, "app.image", "DefaultActuator")
        component.sortValue = getSetting(component.settings, "app.position_index", 0);

        if (component.template === undefined) {
            if (component.status["motion_detection.state"] !== undefined) {
                component.template = "views/motionDetectorTemplate.html";

                if (isFirstRun) {
                    c.motionDetectors.push(component);
                }
            }
            else if (component.status["button.state"] !== undefined) {
                component.template = "views/buttonTemplate.html";

                if (isFirstRun) {
                    c.motionDetectors.push(component);
                }
            }
            else if (component.status["temperature.value"] !== undefined) {
                component.template = "views/temperatureSensorTemplate.html";

                if (isFirstRun) {
                    c.sensors.push(component);
                }
            }
            else if (component.status["humidity.value"] !== undefined) {
                component.template = "views/humiditySensorTemplate.html";

                component.dangerValue = getSetting(source.settings, "app.humidity.danger_value", 75);
                component.warningValue = getSetting(source.settings, "app.humidity.warning_value", 60);

                if (isFirstRun) {
                    c.sensors.push(component);
                }
            }
            else if (component.status["roller_shutter.state"] !== undefined) {
                component.template = "views/rollerShutterTemplate.html";

                if (isFirstRun) {
                    c.rollerShutters.push(component);
                }
            }
            else if (component.status["state_machine.state"] !== undefined) {
                component.template = "views/stateMachineTemplate.html";
            }
            else if (component.status["level.current"] !== undefined) {
                component.template = "views/fanTemplate.html";
            }
            else if (component.status["power.state"] !== undefined) {
                component.template = "views/toggleTemplate.html";
            }
        }
    }

    c.configureArea = function (area, areaStatus, status, isFirstRun) {
        area.settings = areaStatus.settings;
        area.status = areaStatus.status;

        area.caption = getSetting(area.settings, "app.caption", "#" + area.uid);
        area.sortValue = getSetting(area.settings, "app.position_index", 0);
        area.isVisible = getSetting(area.settings, "app.is_visible", true);

        $.each(area.components, function (i, component) {
            var componentStatus = status.components.find(x => x.uid == component.uid);
            if (componentStatus === undefined) {
                return;
            }

            c.configureComponent(component, area, componentStatus, isFirstRun);
        });
    }

    // Start the polling loop for new status.
    apiService.newStatusReceivedCallback = c.applyNewStatus;
    apiService.pollStatus();
}

function getSetting(source, name, defaultValue) {
    if (source === undefined) {
        return defaultValue;
    }

    var value = source[name];

    if (value === undefined || value === null) {
        return defaultValue;
    }

    return value;
}