const mqtt = require('mqtt');

console.log("Connecting to MQTT...");
const client  = mqtt.connect({ clientId: "webserver", protocolVersion: 5 });
client.on('connect', () => {
    console.log("Connected to MQTT");
});
client.subscribe('ui/resp', err => {
    if (err) {
        console.error("Can't subscribe: " + err.message);
        throw new Error("Can't subscribe: " + err.message);
    }
});

const sendRpc = (topic, msg) => {
    let unsub;
    return new Promise((resolve, reject) => {
        // Make request to server
        const correlationData = Buffer.from(`C${Math.random()}`);
        const onMessage = (topic, payload, packet) => {
            if (topic === "ui/resp" && packet?.properties?.correlationData.toString() === correlationData.toString()) {
                resolve(payload.toString());
                unsub();
            }
        };
        client.on('message', onMessage);
        unsub = () => {
            client.off('message', onMessage);
        };
        client.publish(topic, msg, { properties: { responseTopic: "ui/resp", correlationData } }, err => {
            if (err) {
                reject(new Error("Can't publish request: " + err.message));
                unsub();
            }
        });
    });
};

const remoteCall = async (topic, msg) => {
    if (client.disconnected) {
        throw new Error("Broker disconnected");
    }
    return JSON.parse(await sendRpc(topic, JSON.stringify(msg)));
};

module.exports = { remoteCall };
