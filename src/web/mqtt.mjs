import mqtt from 'mqtt';
import { logger } from './settings.mjs';

logger("Connecting to MQTT...");
const client  = mqtt.connect({ clientId: "webserver", protocolVersion: 5 });

client.on('connect', () => {
    logger("Connected to MQTT");
    client.subscribe('ui/resp', err => {
        if (err) {
            console.error("Can't subscribe: " + err.message);
            throw new Error("Can't subscribe: " + err.message);
        }
    });
});

client.on('disconnect', () => {
    logger("Disconnected from MQTT, reconnecting...");
    setTimeout(() => {
        client.reconnect();
    }, 4000);
});

const msgs = { };
let msgIdx = 0;

client.on('message', (topic, payload, packet) => {
    if (topic === "ui/resp") {
        const correlationData = packet.properties?.correlationData.toString();
        const msg = msgs[correlationData];
        if (msg) {
            delete msgs[correlationData];
            if (packet.properties?.contentType === "application/net_err+text") {
                msg.reject(new Error(payload.toString()));
            } else {
                msg.resolve(payload.toString());
            }
        }
    }
});

export const rawRemoteCall = (topic, payload) => {
    if (!client.connected) {
        throw new Error("Broker disconnected");
    }
    // Make request to server
    const correlationData = Buffer.from(`C${msgIdx++}`);
    let msg;
    const promise = new Promise((resolve, reject) => {
        msg = { resolve, reject };
    });
    msgs[correlationData] = msg;
    
    client.publish(topic, payload, { properties: { responseTopic: "ui/resp", correlationData } }, err => {
        if (err) {
            msg.reject(new Error(`Can't publish request: ${err.message}`));
        }
    });

    const timeout = new Promise((_, reject) => {
        setTimeout(() => reject(new Error("Timeout contacting the remote process")), 3500);
    });

    return Promise.race([promise, timeout]);
};

export const jsonRemoteCall = async (res, topic, json) => {
    try {
        const resp = JSON.parse(await rawRemoteCall(topic, JSON.stringify(json)));
        res.send(resp);
    } catch (err) {
        res.status(500).send(err.message);
    }
};
