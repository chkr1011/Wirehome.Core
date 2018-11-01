def on_startup_completed():
    import_parameters = {
        "server": "192.168.1.16",
        "port": 1884,
        "topic": "#"
    }

    mqtt.start_topic_import("heating-server", import_parameters)

    import_parameters = {
        "server": "192.168.1.16",
        "port": 1883,
        "topic": "garden_controller/$STATUS/#"
    }

    mqtt.start_topic_import("ha4iot-garden", import_parameters)

    log.info("startup script executed")