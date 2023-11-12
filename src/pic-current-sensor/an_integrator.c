#include <xc.h>
#include <pic-modbus/modbus.h>
#include "an_integrator.h"

// Sample loop 4 times per seconds and accumulate each value in 32-bit value (signed so 31).
// This means that, at max reading (10 bit), you have 2^(31-10)/4 seconds to read accumulator before overflow (~145h)
// However count is 16 bit unsigned, so it resets first (after 2^16/4 seconds, ~273 minutes in case of no readings)
// See also Microchip datasheet for minimum period (16.4 A/D Acquisition Requirements)
#define ACQUISITION_LOOP_PERIOD (TICKS_PER_SECOND / 4)
#define ACQUISITION_PERIOD (ACQUISITION_LOOP_PERIOD / AN_CHANNELS)

static ANALOG_INTEGRATOR_DATA _data;
static enum {
    // Idle and acquisition time
    IDLE,
    // Waiting for ADC sampling data
    SAMPLING
} _state;

static TICK_TYPE _acquisitionStartTs;
static uint8_t _channel;
static ANALOG_INTEGRATOR_CHANNEL_DATA* _channelPtr;

static void resetData() {
    _channelPtr = &_data.ch[0];
    for (uint8_t i = 0; i < AN_CHANNELS; i++, _channelPtr++) {
        _channelPtr->accumulator = 0;
        _channelPtr->count = 0;
    }
}

static void startAcquire() {
    _channel++;
    _channelPtr++;
    if (_channel >= AN_CHANNELS) {
        _channel = 0;
        _channelPtr = &_data.ch[0];
    }

    switch (_channel) {
        case 0:
            ADCON0bits.CHS = 11;    // Analog Channel Select bits: AN11 -> RB1
            break;
        case 1:
            ADCON0bits.CHS = 9;    // Analog Channel Select bits: AN9 -> RB3
            break;
    }
    NOP();
    ADCON0bits.ADON = 1;    // ADC is enabled
    _acquisitionStartTs = timers_get();
}

void anint_init() {
    resetData();
    _state = IDLE;
    _channel = 0;
    
    // FVR: Fixed voltage reference
    // Since the LTC1966 RMS converter module range is 1Vpeak in input (so ~0.7V out), uses the internal 1.024V ref for ADC.
    FVRCONbits.ADFVR = 1; // ADC Fixed Voltage Reference Peripheral output is 1x (1.024V)
    FVRCONbits.CDAFVR = 0; // Comparator and DAC Fixed Voltage Reference Peripheral output is off.
    FVRCONbits.FVREN = 1; // Fixed Voltage Reference is enabled
    // Wait FVR ready
    while (!FVRCONbits.FVRRDY);
    
    // Configure RB1 (AN11) as analog input channel 0
    ANSELBbits.ANSB1 = 1;
    TRISBbits.TRISB1 = 1;

    // Configure RB3 (AN9) as analog input channel 1
    ANSELBbits.ANSB3 = 1;
    TRISBbits.TRISB3 = 1;

    ADCON1bits.ADNREF = 0;  // VREF- is connected to VSS  
    ADCON1bits.ADPREF = 3;  // VREF+ is connected to internal Fixed Voltage Reference (FVR) module
    ADCON1bits.ADFM = 1; // Right justified. Six Most Significant bits of ADRESH are set to ?0? when the conversion result is loaded
    ADCON1bits.ADCS = 7; // FRC (clock supplied from a dedicated RC oscillator), slower, but it support sleep mode

    startAcquire();
}

void anint_poll() {
    switch (_state) {
        case IDLE: {
            TICK_TYPE now = timers_get();
            if ((now - _acquisitionStartTs) > ACQUISITION_PERIOD) {
                _state = SAMPLING;

                // Start sampling
                ADCON0bits.GO = 1; // A/D conversion cycle in progress. Setting this bit starts an A/D conversion cycle. This bit is automatically cleared by hardware when the A/D conversion has completed.
            }
            break;
        }
        case SAMPLING: {
            // Check if sample is done
            // if done, read it and 
            if (!ADCON0bits.GO) {
                // Read ADC RESult registers
                uint16_t value = (uint16_t)((ADRESH << 8) + ADRESL);
                _channelPtr->accumulator += value;
                _channelPtr->count++;
                if (_channelPtr->accumulator < 0 || _channelPtr->count == 0) {
                    // Overflow
                    sys_fatal(ERR_DEVICE_DEADLINE_MISSED);
                }
                
                _state = IDLE;
                // Move to next channel
                startAcquire();
            }            
        }
    }
}

void anint_read(ANALOG_INTEGRATOR_DATA* data) {
    *data = _data;
    resetData();
}
