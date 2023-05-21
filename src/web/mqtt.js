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
            if (topic === "ui/resp" && packet.properties?.correlationData.toString() === correlationData.toString()) {
                if (packet.properties?.contentType === "application/net_err+text") {
                    reject(new Error(payload.toString()));
                } else {
                    resolve(payload.toString());
                }
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
    try {
        const ret = await sendRpc(topic, JSON.stringify(msg));
        return JSON.parse(ret);
    } catch (err) {
        return { err: err.message };
    }
};

module.exports = { remoteCall };
