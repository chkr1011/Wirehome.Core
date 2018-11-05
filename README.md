<p align="center">
<img src="https://github.com/chkr1011/Wirehome.Core/blob/master/docs/images/gitHubLogo.png?raw=true" width="256">
<br/>
<br/>
</p>

[![BCH compliance](https://bettercodehub.com/edge/badge/chkr1011/MQTTnet?branch=master)](https://bettercodehub.com/)

# Wirehome.Core
This is an open source Home Automation system for .NET Core. This system is written in C# targeting .NET Standard 2.0+- It runs on Windows and Linux (e.g. Raspberry Pi 2+). 

The interaction with the physical home automation hardware is done via _adapters_ which are written in Python. They are available in a separate repository and can be downloaded into the local instance of Wirehome.Core. The backend for the python code is _IronPython_.

The python adapters can access a wide range of modules which are providing access to `GPIOs`, `I2C`, `MQTT` (with a built-in broker), `HTTP ` etc.

Also automations and hardware services are written in Python. The integration of _Open Weather Map_ is completely written in Python and can be downloaded from the official _Wirehome.Repositories_. Also version updates are distributed from the repositories.

A Web App which is hosted by Wirehome.Core is also part of this project. It runs on nearly all current devices like smartphones, tablets etc.

Please visit the Wiki for more details etc.
