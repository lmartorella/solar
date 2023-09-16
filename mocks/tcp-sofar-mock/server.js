import modbus from 'jsmodbus';
import net from 'net';

const netServer = new net.Server();
const server = new modbus.server.TCP(netServer, { holding: Buffer.alloc(0x2000 * 2)});
netServer.listen(502);
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
const maxTotalPower = 5800;
const minTotalPower = 800;
const period = 10 * 60; // In seconds
const internalUpdateInterval = 500; // In ms

const startTime = new Date();
let totalCharge = 0; // in Ah
let totalChargeLastUpdate = startTime;

const writeReg = (regAddress, value) => {
    regAddress = Array.isArray(regAddress) ? regAddress : [regAddress];
    regAddress.forEach(address => {
        server.holding.writeUInt16BE(Math.round(value), address * 2);
    });
};

setInterval(() => {
    const now = new Date();
    const elapsedSeconds = (now - startTime) / 1000;
    const totalPower = ((Math.sin(elapsedSeconds / period * 2 * Math.PI) + 1) / 2) * (maxTotalPower - minTotalPower) + minTotalPower;
    const stringPowers = [totalPower * string1Ratio, totalPower * (1 - string1Ratio)];

    const updatePower = (addresses, power, voltage) => {
        writeReg(addresses.voltage, voltage * 10);
        writeReg(addresses.current, power / voltage * 100);
        writeReg(addresses.power, power / 10);
    };

    updatePower(addresses.grid, totalPower, gridVoltage);
    updatePower(addresses.string1, stringPowers[0], 290 + Math.random() * 20);
    updatePower(addresses.string2, stringPowers[1], 190 + Math.random() * 20);

    const gridCurrent = totalPower / gridVoltage;
    totalCharge += gridCurrent * ((now - totalChargeLastUpdate) / 1000 / 60 / 60);
    totalChargeLastUpdate = now;

    writeReg(addresses.totalChargeToday, totalCharge / 2 * 10);

}, internalUpdateInterval);

console.log("Serving at port 502 (modbus)");
