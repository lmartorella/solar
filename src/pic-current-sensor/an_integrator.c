#include <xc.h>
#include <pic-modbus/modbus.h>
#include "an_integrator.h"

// Sample the A/D (10bit) 4 times per seconds (for each channel) and accumulate each value in 16-bit unsigned value.
// The average result (accumulator / count) is stored in a fixed-point decimal register.
// See also Microchip datasheet for minimum period (16.4 A/D Acquisition Requirements)
#define ACQUISITIONS_PER_SECONDS (4)
#define ACQUISITION_LOOP_PERIOD (TICKS_PER_SECOND / ACQUISITIONS_PER_SECONDS)
#define ACQUISITION_PER_CHANNEL_PERIOD (ACQUISITION_LOOP_PERIOD / AN_CHANNELS)

typedef struct {
    // Calibration data, cached in RAM. When set to 2^15, it is the neutral calibration.
    uint16_t calibration;
} ANALOG_INTEGRATOR_ACCUMULATOR;

// The integrated A/D value for the last period. Every single reading is an unsigned 10bits.
static uint16_t _accumulators[AN_CHANNELS];
static ANALOG_INTEGRATOR_DATA _values;
static ANALOG_INTEGRATOR_CALIBRATION _calibration;

static enum {
    // Idle and acquisition time
    IDLE,
    // Waiting for ADC sampling data
    SAMPLING
} _state;

static TICK_TYPE _acquisitionStartTs;
static uint8_t _channel;
static uint8_t _count;

static void storeValuesAndReset() {
    for (uint8_t i = 0; i < AN_CHANNELS; i++) {
        // By default calibration is 2^15 (32k), so shift an additional bit to have 16+16 fixed decimal
        _values.values[i] = (((uint32_t)_accumulators[i] * (uint32_t)_calibration.calib[i]) << 1) / _count;
        _accumulators[i] = 0;
    }
    _count = 0;
}

static void startAcquire() {
    _channel++;
    if (_channel >= AN_CHANNELS) {
        _channel = 0;
        _count++;
        if (_count >= ACQUISITIONS_PER_SECONDS) {
            storeValuesAndReset();
        }
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

// Default values stored in EEPROM
__eeprom ANALOG_INTEGRATOR_CALIBRATION calibrationValuesEeprom = { 0x8000, 0x8000 };
            
void anint_init() {
    _count = 0;
    _state = IDLE;
    _channel = 0;
    
    for (uint8_t i = 0; i < AN_CHANNELS; i++) {
        _accumulators[i] = 0;
        _calibration.calib[i] = calibrationValuesEeprom.calib[i];
    }
    
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
            if ((timers_get() - _acquisitionStartTs) > ACQUISITION_PER_CHANNEL_PERIOD) {
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
                _accumulators[_channel] += value;
                _state = IDLE;
                // Move to next channel
                startAcquire();
            }            
        }
    }
}

void anint_read_values(ANALOG_INTEGRATOR_DATA* data) {
    *data = _values;
}

void anint_read_calib(ANALOG_INTEGRATOR_CALIBRATION* data) {
    *data = _calibration;
}

void anint_write_calib(ANALOG_INTEGRATOR_CALIBRATION* data) {
    _calibration = *data;
    unsigned short addr = (unsigned short)&calibrationValuesEeprom;
    uint8_t* src = (uint8_t*)&_calibration;
    for (int i = 0; i < sizeof(ANALOG_INTEGRATOR_CALIBRATION); i++) {
        eeprom_write(addr++, *(src++));
    }
}
