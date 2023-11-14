#ifndef _AN_INTEGRATOR_H
#define _AN_INTEGRATOR_H

typedef struct {
    // The integrated A/D value for the last second, 16 integer + 16 fractional bits (value * 65536)
    uint32_t value;
} ANALOG_INTEGRATOR_CHANNEL_DATA;

// Read two inputs
#define AN_CHANNELS (2)

typedef struct {
    ANALOG_INTEGRATOR_CHANNEL_DATA ch[AN_CHANNELS];
} ANALOG_INTEGRATOR_DATA;

void anint_init(void);
void anint_poll(void);

// Read data and reset counters
void anint_read(ANALOG_INTEGRATOR_DATA* data);

#endif
