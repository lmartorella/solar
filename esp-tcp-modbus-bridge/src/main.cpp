#if defined(ESP8266)
#include <ESP8266WiFi.h>
#else
#include <WiFi.h>
#endif

#if defined(ESP32)
// Not compatible with Arduino UDP of esp8266 core yet
#include <WiFiUdp.h>
#include <ArduinoMDNS.h>
#endif

#include <HardwareSerial.h>
#include <EspModbusBridge.h>
#include "wifi_ssid.h"

#if defined(ESP8266)
static HardwareSerial& _rtuSerial = Serial;
#else
static HardwareSerial _rtuSerial(1);
#endif

#if defined(ESP32)
static WiFiUDP udp;
static MDNS mdns(udp);
#endif

class SofarModbusBridge : public TelnetModbusBridge { 
protected:
    /**
     * Fix frame errors of the first byte due to bus arbitration/drive switch 
     */
    virtual void tryFixFrame(uint8_t rtuNodeId, uint8_t requestFunction, Modbus::frame_arg_t* frameArg, uint8_t*& data, uint8_t& len);
};

static TelnetModbusBridge bridge;

void setup() {
#if defined(ESP32)
    // Keep Serial1 healthy for debugging
    Serial.begin(115200, SERIAL_8N1);
    Serial.end();
    Serial.begin(115200, SERIAL_8N1);
#endif

#if defined(ESP8266)
    _rtuSerial.begin(9600, SERIAL_8N1);
#else
    _rtuSerial.begin(9600, SERIAL_8N1, 35, 33); // 35 is input only
#endif

    WiFi.setHostname(WIFI_HOSTNAME);
    WiFi.begin(WIFI_SSID, WIFI_PASSPHRASE);
    Serial.printf("Connected\n");

#if defined(ESP32)
    // Initialize the mDNS library. You can now reach or ping this
    // Arduino via the host name "arduino.local", provided that your operating
    // system is mDNS/Bonjour-enabled (such as MacOS X).
    // Always call this before any other method!
    mdns.begin(WiFi.localIP(), WIFI_HOSTNAME);
#endif

#if defined(ESP8266)
    bridge.begin(_rtuSerial, 0, TxEnableHigh);
#else
    bridge.begin(_rtuSerial, 32, TxEnableHigh);
#endif
}

static unsigned long previousMillis = 0;
const unsigned long interval = 30000;
static void reconnectTask() {
  unsigned long currentMillis = millis();
  // if WiFi is down, try reconnecting
  if ((WiFi.status() != WL_CONNECTED) && (currentMillis - previousMillis >= interval)) {
    Serial.print(millis());
    Serial.println("Reconnecting to WiFi...");
    WiFi.disconnect();
    WiFi.reconnect();
    previousMillis = currentMillis;
  }
}

void loop() {
#if defined(ESP32)
    // This actually runs the mDNS module. YOU HAVE TO CALL THIS PERIODICALLY,
    // OR NOTHING WILL WORK! Preferably, call it once per loop().
    mdns.run();
#endif

    bridge.task();
    reconnectTask();
    yield();
}

/**
 * Fix frame errors of the first byte due to bus arbitration/drive switch 
 */
void SofarModbusBridge::tryFixFrame(uint8_t rtuNodeId, uint8_t requestFunction, Modbus::frame_arg_t* frameArg, uint8_t*& data, uint8_t& len) {
  if (len == 3 && data[1] == 0x90) {
    // Shift 1
    len--;
    data++;
  }
  if (len == 2 && data[0] == 0x90) {
    // Fix Sofar error
    data[0] = 0x83;
    frameArg->validFrame = true;
    frameArg->slaveId = rtuNodeId;
    log.printf("Recovered 0x90 error\n");
  } else if (data[0] == rtuNodeId && data[1] == requestFunction) {
    // 1-shift is common, the node Id entered in the frame shifting everything up
    len--;
    data++;
    frameArg->validFrame = true;
    frameArg->slaveId = rtuNodeId;
    log.printf("Recovered 1-byte shifted frame\n");
  } else {
      log.printf("RTU: Invalid frame: ");
      uint8_t i;
      for (i = 0; i < len + 2 && i < 64; i++) {
        log.printf("<%02x>", data[i]);
      }
      if (i >= 64) {
        log.printf("...");
      }
      log.printf("\n");
  }
}
