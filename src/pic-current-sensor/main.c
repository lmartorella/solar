#include <xc.h>
#include <pic-modbus/modbus.h>
#include "an_integrator.h"

#define LE_TO_BE_16(v) (((v & 0xff) << 8) + (v >> 8))

/**
 * Holding registers [0-2]
 */
typedef struct {
    /**
     * See SYS_RESET_REASON
     */
    uint8_t resetReason;
    uint8_t _filler1;
    
    /**
     * Count of CRC errors in the reading period
     */
    uint8_t crcErrors;
    uint8_t _filler2;
} SYS_REGISTERS;

#define SYS_REGS_ADDRESS_BE (LE_TO_BE_16(0x0))
#define SYS_REGS_COUNT (sizeof(SYS_REGISTERS) / 2)

#define SENSORS_REGS_COUNT (sizeof(ANALOG_INTEGRATOR_DATA) / 2)
#define SENSORS_REGS_ADDRESS_BE (LE_TO_BE_16(0x200))

_Bool regs_validateReg() {
    uint8_t count = bus_cl_header.address.countL;
    uint16_t addressBe = bus_cl_header.address.registerAddressBe;
    
    // Exposes the system registers in the rane 0-2
    if (addressBe == SYS_REGS_ADDRESS_BE) {
        if (count != SYS_REGS_COUNT) {
            bus_cl_exceptionCode = ERR_INVALID_SIZE;
            return false;
        }
        return true;
    }
   
    if (addressBe == SENSORS_REGS_ADDRESS_BE) {
        // Only reading whole channel range at a time is supported
        if (count != SENSORS_REGS_COUNT) {
            bus_cl_exceptionCode = ERR_INVALID_SIZE;
            return false;
        }
        if (bus_cl_header.header.function != READ_HOLDING_REGISTERS) {
            bus_cl_exceptionCode = ERR_INVALID_FUNCTION;
            return false;
        }
        return true;
    }
    
    bus_cl_exceptionCode = ERR_INVALID_ADDRESS;
    return false;
}

_Bool regs_onReceive() {
    uint16_t addressBe = bus_cl_header.address.registerAddressBe;
    if (addressBe == SYS_REGS_ADDRESS_BE) {
        // Ignore data, reset flags and counters
        sys_resetReason = RESET_NONE;
        bus_cl_crcErrors = 0;
        return true;
    }
    return false;
}

void regs_onSend() {
    uint16_t addressBe = bus_cl_header.address.registerAddressBe;
    if (addressBe == SYS_REGS_ADDRESS_BE) {
        ((SYS_REGISTERS*)rs485_buffer)->crcErrors = bus_cl_crcErrors;
        ((SYS_REGISTERS*)rs485_buffer)->resetReason = sys_resetReason;
        return;
    }
    
    if (addressBe == SENSORS_REGS_ADDRESS_BE) {
        anint_read_values((ANALOG_INTEGRATOR_DATA*)rs485_buffer);
        return;
    }
}

void __interrupt() low_isr() {
    timers_isr();
}

static void enableInterrupts() {
    // Disable low/high interrupt mode
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
}

void main() {
    // Analyze RESET reason
    sys_init();
    modbus_init();
    anint_init();
    enableInterrupts();
    // I'm alive
    while (1) {
        CLRWDT();
        modbus_poll();
        anint_poll();
    }
}
