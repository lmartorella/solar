#ifndef _AN_INTEGRATOR_H
#define _AN_INTEGRATOR_H

// Supports two A/D channels (AN11 and AN9)
#define AN_CHANNELS (2)

typedef struct {
    // The integrated A/D value for the last second, 16 integer + 16 fractional bits (value * 65536)
    uint32_t values[AN_CHANNELS];
} ANALOG_INTEGRATOR_DATA;

typedef struct {
    // The calibration data, stored in EEPROM
    // Calibration data. When set to 2^15, it is the neutral calibration.
    uint16_t calib[AN_CHANNELS];
} ANALOG_INTEGRATOR_CALIBRATION;

void anint_init(void);
void anint_poll(void);

// Read data, sampled at 1 second
void anint_read_values(ANALOG_INTEGRATOR_DATA* data);

// Read/write calibration data
void anint_read_calib(ANALOG_INTEGRATOR_CALIBRATION* data);
void anint_write_calib(ANALOG_INTEGRATOR_CALIBRATION* data);

#endif
