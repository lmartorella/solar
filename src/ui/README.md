# Web application

## Library

The `lib` folder contains the Angular library that encapsulate the UI.

It shows solar panel real-time statistics and charts, up to 4 days in the past, and power meter data.

The `index.mjs` file instead is a Node.JS module for Express JS that implement the server-side REST API to communicate with MQTT.

The library is meant to be loaded in a portal that contains other home automation UIs as well.

## Sample

The `sample` folder implements a very simple web app that shows a bare minimum UI for the solar library.

Use `ng serve` to serve it in Angular server, and `npm run express` to start the REST API.
