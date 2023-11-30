#if defined(ESP8266)
#include <ESP8266WiFi.h>
#else
#include <WiFi.h>
#endif

#include <HardwareSerial.h>
#include <EspModbusBridge.h>
#include "wifi_ssid.h"

#if defined(ESP8266)
static HardwareSerial& _rtuSerial = Serial;
#else
static HardwareSerial _rtuSerial(1);
#endif

static TelnetModbusBridge bridge;

void setup() {
#if defined(ESP32)
    // Keep Serial1 healthy for debugging
    Serial.begin(115200, SERIAL_8N1);
    Serial.end();
    Serial.begin(115200, SERIAL_8N1);
#endif

#if defined(ESP8266)
    _rtuSerial.begin(19200, SERIAL_8N1);
#else
    _rtuSerial.begin(19200, SERIAL_8N1, 35, 33); // 35 is input only
#endif

    WiFi.setHostname(WIFI_HOSTNAME);
    WiFi.begin(WIFI_SSID, WIFI_PASSPHRASE);

#if defined(ESP8266)
    bridge.begin(_rtuSerial, 0, TxEnableHigh);
#else
    bridge.begin(_rtuSerial, 32, TxEnableHigh);
#endif
}

void loop() {
    bridge.task();
    yield();
}
