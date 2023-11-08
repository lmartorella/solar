#ifndef _AN_INTEGRATOR_H
#define _AN_INTEGRATOR_H

typedef struct {
    // The integrated A/D value for the last period. Every accumulated reading is an unsigned 10bits.
    int32_t value;
    // The count of samples accumulated in `value` so far
    uint16_t count;
} ANALOG_INTEGRATOR_DATA;

void anint_init(void);
void anint_poll(void);

// Read data and reset counters
void anint_read(ANALOG_INTEGRATOR_DATA* data);

#endif
