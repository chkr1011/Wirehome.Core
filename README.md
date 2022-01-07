<p align="center">
<img src="https://github.com/chkr1011/Wirehome.Core/blob/master/docs/images/gitHubLogo.png?raw=true" width="256">
<br/>
<br/>
</p>

[![BCH compliance](https://bettercodehub.com/edge/badge/chkr1011/MQTTnet?branch=master)](https://bettercodehub.com/) [![Join the chat at https://gitter.im/Wirehome-Core/community](https://badges.gitter.im/Wirehome-Core/community.svg)](https://gitter.im/Wirehome-Core/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Wirehome.Core
Wirehome.Core is an open source Home Automation system for .NET Core. This system is written in C# targeting .NET Standard 2.0+- It runs on Windows, macOS and Linux (e.g. a Raspberry Pi 2 Model B+). 

The interaction with the physical home automation hardware is abstracted via _adapters_ which are written in Python. They are available in a separate repository and can be downloaded into the local instance of Wirehome.Core. The engine for the Python code is _IronPython_.

The Python adapters can access a wide range of modules (Wirehome.API) which are providing access to `GPIOs`, `I2C`, `MQTT`, `HTTP`, `COAP` etc.

Wirehome.Core also includes a fully features MQTT broker and a HTTP server which can be used to host user content or for interaction with the devices.

Also automations and custom services are written in Python. The integration of _Open Weather Map_ is completely written in Python and can be downloaded from the official _Wirehome.Repositoriy_. Also version updates are distributed from that repository.

# Wirehome.App
A Web App which is hosted by Wirehome.Core is also part of this project. It runs on nearly all current devices like smartphones, tablets, PCs etc.

<p align="center">
<img src="https://github.com/chkr1011/Wirehome.Core/blob/master/docs/images/app_screen_1.png?raw=true" width="256">
<img src="https://github.com/chkr1011/Wirehome.Core/blob/master/docs/images/app_screen_2.png?raw=true" width="256">
<img src="https://github.com/chkr1011/Wirehome.Core/blob/master/docs/images/app_screen_3.png?raw=true" width="256">
</p>
