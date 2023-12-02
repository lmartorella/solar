#ifndef _AN_INTEGRATOR_H
#define _AN_INTEGRATOR_H

// Supports two A/D channels (AN11 and AN9)
#define AN_CHANNELS (2)

typedef struct {
    // The integrated A/D value for the last second, 16 integer + 16 fractional bits (value * 65536)
    // as LSB fixed point values
    uint32_t values[AN_CHANNELS];
} ANALOG_INTEGRATOR_DATA;

void anint_init(void);
void anint_poll(void);

// Read data, sampled at 1 second
void anint_read_values(ANALOG_INTEGRATOR_DATA* data);

#endif
