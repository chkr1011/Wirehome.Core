function createComponentService(apiService) {
    var srv = this;

    srv.enable = function (component) {
        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", true);
    }

    srv.disable = function (component) {
        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", false);
    }

    srv.toggleIsEnabled = function (component) {
        if (component.getSetting("is_enabled", true) === true) {
            srv.disable(component);
        } else {
            srv.enable(component);
        }
    };

    srv.togglePowerState = function (component) {
        var parameters = {}
        parameters["type"] = "toggle";
        srv.sendCommand(component, parameters);
    }

    srv.turnOn = function (component) {
        var parameters = {}
        parameters["type"] = "turn_on";
        srv.sendCommand(component, parameters);
    }

    srv.turnOff = function (component) {
        var parameters = {}
        parameters["type"] = "turn_off";
        srv.sendCommand(component, parameters);
    }

    srv.setLevel = function (component, level) {
        var parameters = {}
        parameters["type"] = "set_level";
        parameters["level"] = level;
        srv.sendCommand(component, parameters);
    }

    srv.increaseLevel = function (component) {
        var parameters = {}
        parameters["type"] = "increase_level";
        srv.sendCommand(component, parameters);
    }

    srv.decreaseLevel = function (component) {
        var parameters = {}
        parameters["type"] = "decrease_level";
        srv.sendCommand(component, parameters);
    }

    srv.moveUp = function (component) {
        var parameters = {}
        parameters["type"] = "move_up";
        srv.sendCommand(component, parameters);
    }

    srv.moveDown = function (component) {
        var parameters = {}
        parameters["type"] = "move_down";
        srv.sendCommand(component, parameters);
    }

    srv.setColor = function (component, hexValue) {
        var rgb = srv.hexToRgb(hexValue);

        var parameters = {}
        parameters.type = "set_color";
        parameters.format = "rgb";
        parameters.r = rgb.r;
        parameters.g = rgb.g;
        parameters.b = rgb.b;

        srv.sendCommand(component, parameters);
    }

    srv.pressButton = function (component, duration) {
        var parameters = {}
        parameters["type"] = "press";
        parameters["duration"] = duration;
        srv.sendCommand(component, parameters);
    }

    srv.setState = function (component, state) {
        var parameters = {}
        parameters["type"] = "set_state";
        parameters["state"] = state;
        srv.sendCommand(component, parameters);
    }

    srv.sendCommand = function (component, message) {
        apiService.executePost("/api/v1/components/" + component.uid + "/process_message", message, function (response) {
            if (response["type"] !== "success") {
                delete response["component"];
                alert(JSON.stringify(response));

                return;
            }

            if (srv.componentUpdatedCallback !== undefined) {
                srv.componentUpdatedCallback(response["component"])
            }
        });
    }

    srv.hexToRgb = function (hex) {
        var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : null;
    }

    srv.rgbToHex = function rgbToHex(r, g, b) {
        return "#" + numberToHex(r) + numberToHex(g) + numberToHex(b);
    }

    function numberToHex(number) {
        var hex = number.toString(16);
        return hex.length == 1 ? "0" + hex : hex;
    }

    return this;
}