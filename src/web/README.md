# The web server

This simple [Node.JS](https://nodejs.org/) web server is designed to allow some degree of remote access, mainly for checking status and start/stop/program some devices.

This process is loosely coupled with the main [server](../server/README.md) through an ad-hoc IPC channel for obvious security reasons.

Then, the web access will **not** expose:
- anything about the topology
- direct device/sink accesses
- direct IP/protocol accesses

The generic messages-based protocol implemented here should be paired with logic in the server applicative layer.

## Server interaction

This Node.JS application can be even used to remotely start/stop/automatically restart the server process.

For these operations a login is required (implemented via [Express.js](https://expressjs.com/) authentication methods).

## A sample web app

See [here](../../samples/web/README.md) for a sample web app.
