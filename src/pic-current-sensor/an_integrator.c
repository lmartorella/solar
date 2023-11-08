#include <xc.h>
#include <pic-modbus/modbus.h>
#include "an_integrator.h"

static ANALOG_INTEGRATOR_DATA _data;
static enum {
    IDLE,
    SAMPLING
} _state;
static TICK_TYPE _lastSample;

static void resetData() {
    _data.ch0.accumulator = 0;
    _data.ch0.count = 0;
    _data.ch1.accumulator = 0;
    _data.ch1.count = 0;
}

void anint_init() {
    resetData();
    _state = IDLE;
    _lastSample = timers_get();
    
    // Uses RB1, range from 0V to 1.024V
    ANSELBbits.ANSB1 = 1;
    TRISBbits.TRISB1 = 1;
    FVRCONbits.ADFVR = 1;
    FVRCONbits.CDAFVR = 0;
    FVRCONbits.FVREN = 1;
    while (!FVRCONbits.FVRRDY);
    ADCON0bits.CHS = 11;
    ADCON1bits.ADNREF = 0;
    ADCON1bits.ADPREF = 3;
    ADCON1bits.ADFM = 1; // LSB
    ADCON1bits.ADCS = 7; // internal OSC
    NOP();
    ADCON0bits.ADON = 1;
}

// Sample line 4 times per seconds and accumulate value in 32-bit value (signed so 31).
// This means that, at max reading (10 bit), you have 2^(31-10)/4 seconds to read accumulator before overflow (~145h)
// However count is 16 bit unsigned, so it resets first (after 2^16/4 seconds, ~273 minutes in case of no readings)
void anint_poll() {
    switch (_state) {
        case IDLE: {
            TICK_TYPE now = timers_get();
            if ((now - _lastSample) > (TICKS_PER_SECOND / 4)) {
                _lastSample = now;
                _state = SAMPLING;

                // Start sampling
                ADCON0bits.GO = 1;
            }
            break;
        }
        case SAMPLING: {
            // Check if sample is done
            // if done, read it and 
            if (!ADCON0bits.GO) {
                uint16_t value = (uint16_t)((ADRESH << 8) + ADRESL);
                _data.ch0.accumulator += value;
                _data.ch0.count++;
                if (_data.ch0.accumulator < 0 || _data.ch0.count == 0) {
                    // Overflow
                    sys_fatal(ERR_DEVICE_DEADLINE_MISSED);
                }
                _state = IDLE;
            }            
        }
    }
}

void anint_read(ANALOG_INTEGRATOR_DATA* data) {
    *data = _data;
    resetData();
}
