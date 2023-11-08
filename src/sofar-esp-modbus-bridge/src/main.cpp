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

class SofarModbusBridge : public TelnetModbusBridge { 
protected:
    /**
     * Fix frame errors of the first byte due to bus arbitration/drive switch 
     */
    virtual void tryFixFrame(uint8_t rtuNodeId, uint8_t requestFunction, Modbus::frame_arg_t* frameArg, uint8_t*& data, uint8_t& len) override;
};

static SofarModbusBridge bridge;

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

#if defined(ESP8266)
    bridge.begin(_rtuSerial, 0, TxEnableHigh);
#else
    bridge.begin(_rtuSerial, 32, TxEnableHigh);
#endif

    // Sofar doesn't follow the modbus spec, and it is splitting messages with more than 3.5 * space time sometimes
    // ((1 / 9600) * 11) * 3.5 = 4ms
    // Use 8ms instead
    bridge.setInterFrameTime(8000);
}

void loop() {
    bridge.task();
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
    data[0] = 0x80 | requestFunction;
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
