function createComponentService(apiService) {
    var srv = this;

    srv.enable = function (component) {
        console.log("Enabling component " + component.uid);

        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", true);
    }

    srv.disable = function (component) {
        console.log("Disabling component " + component.uid);

        apiService.executePost("/api/v1/components/" + component.uid + "/settings/is_enabled", false);
    }

    srv.togglePowerState = function (component) {
        var parameters = {}
        parameters["type"] = "toggle";
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

    srv.setColor = function (component, hue, saturation, value) {
        var parameters = {}
        parameters["type"] = "set_color";
        parameters["hue"] = hue;
        parameters["saturation"] = saturation;
        parameters["value"] = value;
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

    srv.sendCommand = function(component, parameters){
        var type = parameters["type"];
        console.log("Sending command '" + type + "' for component " + component.uid);

        apiService.executePost("/api/v1/components/" + component.uid + "/execute_command", parameters);
    }

    return this;
}