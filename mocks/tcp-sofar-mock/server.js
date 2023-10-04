import modbus from 'jsmodbus';
import net from 'net';

const netServer = new net.Server();
const server = new modbus.server.TCP(netServer, { holding: Buffer.alloc(0x2000 * 2)});

const inverterBuffer = Buffer.alloc(0x2000 * 2);
const ammeterBuffer = Buffer.alloc(0x2000 * 2);

netServer.listen(502);

server.on("preReadHoldingRegisters", request => {
    switch (request.unitId) {
    case 1:
        inverterBuffer.copy(server.holding);
        break;
    case 2:
        ammeterBuffer.copy(server.holding);
        break;
    default:
        throw new Error("Unimplemented unit ID");
    }
});

const addresses = { 
    totalChargeToday: 0x426, // In (Ah / 2) * 10

    grid: {
        voltage: 0x48d, // V * 10
        current: 0x48e, // A + 100
        power: [0x485, 0x488, 0x493],   // W / 10
    },
    string1: {
        voltage: 0x584, // V * 10
        current: 0x585, // A + 100
        power: 0x586    // W / 10
    },
    string2: {
        voltage: 0x587, // V * 10
        current: 0x588, // A + 100
        power: 0x589    // W / 10
    }
};

// Produce a sinusoidal power curve. Fixed grid voltage.
const gridVoltage = 230;
const string1Ratio = 0.4;  // string1/(string1+string2)
const totalFvPowers = { max: 5800, min: 800 };
const homeCurrents = { max: 16, min: 2 };
const fvPeriod = 10 * 60; // In seconds
const ammeterPeriod = 7 * 60; // In seconds
const internalUpdateInterval = 500; // In ms

const startTime = new Date();
let totalCharge = 0; // in Ah
let totalChargeLastUpdate = startTime;

const write16RegBE = (buffer, regAddress, value) => {
    regAddress = Array.isArray(regAddress) ? regAddress : [regAddress];
    regAddress.forEach(address => {
        buffer.writeUInt16BE(Math.round(value), address * 2);
    });
};

const writeFloatRegBE = (buffer, regAddress, value) => {
    regAddress = Array.isArray(regAddress) ? regAddress : [regAddress];
    regAddress.forEach(address => {
        buffer.writeFloatBE(value, address * 2);
    });
};

setInterval(() => {
    const now = new Date();
    const elapsedSeconds = (now - startTime) / 1000;
    const totalPower = ((Math.sin(elapsedSeconds / fvPeriod * 2 * Math.PI) + 1) / 2) * (totalFvPowers.max - totalFvPowers.min) + totalFvPowers.min;
    const stringPowers = [totalPower * string1Ratio, totalPower * (1 - string1Ratio)];

    const updatePower = (addresses, power, voltage) => {
        write16RegBE(inverterBuffer, addresses.voltage, voltage * 10);
        write16RegBE(inverterBuffer, addresses.current, power / voltage * 100);
        write16RegBE(inverterBuffer, addresses.power, power / 10);
    };

    updatePower(addresses.grid, totalPower, gridVoltage);
    updatePower(addresses.string1, stringPowers[0], 290 + Math.random() * 20);
    updatePower(addresses.string2, stringPowers[1], 190 + Math.random() * 20);

    const gridCurrent = totalPower / gridVoltage;
    totalCharge += gridCurrent * ((now - totalChargeLastUpdate) / 1000 / 60 / 60);
    totalChargeLastUpdate = now;

    write16RegBE(inverterBuffer, addresses.totalChargeToday, totalCharge / 2 * 10);

    const homeCurrent = ((Math.sin(elapsedSeconds / ammeterPeriod * 2 * Math.PI) + 1) / 2) * (homeCurrents.max - homeCurrents.min) + homeCurrents.min;
    writeFloatRegBE(ammeterBuffer, 0, homeCurrent);

}, internalUpdateInterval);

console.log("Serving at port 502 (modbus)");
console.log("Serving Sofar inverter at unit ID 1 and ammeter at unit ID 2");