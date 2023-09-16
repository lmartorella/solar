import mqtt from "mqtt";

console.log("Connecting to MQTT...");
const client  = mqtt.connect({ clientId: "solar.webserver" });
const topics = { };

const subscribeAllTopics = () => {
    Object.keys(topics).forEach(topic => {
        if (!topics[topic].subscribed) {
            client.subscribe(topic, err => {
                if (err) {
                    console.error("Can't subscribe: " + err.message);
                    topics[topic].errHandler(new Error("Can't subscribe: " + err.message));
                }
            });
            topics[topic].subscribed = true;
        }
    });
};

client.on("connect", () => {
    console.log("Connected to MQTT");
    subscribeAllTopics();
});

client.on("disconnect", () => {
    console.log("Disconnected from MQTT, reconnecting...");
    Object.keys(topics).forEach(topic => topics[topic].subscribed = false);
    setTimeout(() => {
        client.reconnect();
    }, 4000);
});

client.on("message", (topic, payload) => {
    const handlers = topics[topic];
    if (handlers) {
        let data;
        try {
            data = JSON.parse(payload.toString());
        } catch (err) {
            handlers.errHandler(new Error("Invalid data received"));
            return;
        }
        handlers.dataHandler(data);
    }
});

export const subscribeJsonTopic = (topic, dataHandler, errHandler) => {
    topics[topic] = { dataHandler, errHandler };
    if (client.connected) {
        subscribeAllTopics();
    }
};
