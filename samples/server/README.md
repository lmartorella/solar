# Sample applications: .NET server code

These are the sample projects to build the application logic on the [C# server](../../src/server/README.md).

## Home.Samples

This is a dll library containing some interesting samples:
- a solar panel logger ([Samil Solon](http://www.samilpower.com/) serial protocol).
- a garden irrigation control (designed for [this](../garden/README.md) hardware).
- sample code to interface DHT11 temperature/humidity sink.
- sample code to interface Bosch BPM180 pressure/temperature sink.
- a water flow meter (with reed-based revolution counter)
- digital switch interface (input)
- digital actuator interface (output)

In addition it implements a IPC message-based channel for the [remote web access](../web/README.md). 

## Home.Samples.Simulator.UI

This is a dll library containing the mocked sinks for all of the above samples, to run in the simulator UI.

In addition, there are some testing devices to help diagnose issues with the real hardware.
