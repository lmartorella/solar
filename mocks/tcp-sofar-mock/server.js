import modbus from 'jsmodbus';
import net from 'net';
import { onKeyPress } from "./keypress.js";

const inverterBuffer = Buffer.alloc(0x2000 * 2);
const ammeterBuffer = Buffer.alloc(0x2000 * 2);

let netServer;

let delayNext = false;
let simulateDown = false;

const openSocket = () => {
    if (netServer) {
        return;
    }
    console.log("Open socket");
    netServer = new net.Server();
    const modbusServer = new modbus.server.TCP(netServer, { holding: null });
    netServer.listen(502);

    modbusServer.on("readHoldingRegisters", (request, cb) => {
        const action = simulateDown ? "(dropped)" : (delayNext ? " (delayed)" : "");
        console.log(` Request to unit ${request.unitId} ${action}`);
        if (simulateDown) {
            cb(Buffer.alloc(0));
            return;
        }

        let holding;
        switch (request.unitId) {
            case 1:
                holding = inverterBuffer;
                break;
            case 2:
                holding = ammeterBuffer;
                break;
            default:
                throw new Error("Unimplemented unit ID");
        }
    
        const responseBody = modbus.responses.ReadHoldingRegistersResponseBody.fromRequest(request.body, holding);
        const response = modbus.ModbusTCPResponse.fromRequest(request, responseBody);
        const payload = response.createPayload();
        setTimeout(() => cb(payload), delayNext ? 3000 : 0);
        delayNext = false;
    });
};

const addresses = { 
    status: 0x404,

    totalProdTodayH: 0x684, // In W / 10
    totalProdTodayL: 0x685, // In W / 10
    
    totalProdH: 0x686, // In W / 100
    totalProdL: 0x687, // In W / 100

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
const defGridVoltage = 230;
const string1Ratio = 0.4;  // string1/(string1+string2)
const totalFvPowers = { max: 5800, min: 800 };
const homeCurrents = { max: 16, min: 2 };
const fvPeriod = 10 * 60; // In seconds
const ammeterPeriod = 7 * 60; // In seconds
const internalUpdateInterval = 500; // In ms

const startTime = new Date();
let dailyProdWh = 0; // in Wh
let dailyProdLastUpdate = startTime;

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

    const gridVoltage = defGridVoltage + Math.random() * 10 - 5;
    updatePower(addresses.grid, totalPower, gridVoltage);
    updatePower(addresses.string1, stringPowers[0], 290 + Math.random() * 20);
    updatePower(addresses.string2, stringPowers[1], 190 + Math.random() * 20);

    write16RegBE(inverterBuffer, addresses.status, 2);

    dailyProdWh += totalPower * ((now - dailyProdLastUpdate) / 1000 / 60 / 60);
    dailyProdLastUpdate = now;

    write16RegBE(inverterBuffer, addresses.totalProdTodayH, (dailyProdWh / 10) >> 16);
    write16RegBE(inverterBuffer, addresses.totalProdTodayL, (dailyProdWh / 10) & 0xffff);

    const totalProdWh = 1e6 + dailyProdWh;
    write16RegBE(inverterBuffer, addresses.totalProdH, (totalProdWh / 100) >> 16);
    write16RegBE(inverterBuffer, addresses.totalProdL, (totalProdWh / 100) & 0xffff);

    const homeCurrent = ((Math.sin(elapsedSeconds / ammeterPeriod * 2 * Math.PI) + 1) / 2) * (homeCurrents.max - homeCurrents.min) + homeCurrents.min;
    writeFloatRegBE(ammeterBuffer, 0, homeCurrent);

}, internalUpdateInterval);

console.log("Serving at port 502 (modbus)");
console.log("Serving Sofar inverter at unit ID 1 and ammeter at unit ID 2");

console.log(
`Press:
'q' to quit
'd' to delay the next request of 3 seconds
's' to stop responding to all the requests
'r' to resume responding to all the requests
`);

onKeyPress(key => {
    switch (key) {
        case 'q':
            process.exit(0);
        case 'd':
            if (!delayNext) {
                console.log("Delaying next request...");
                delayNext = true; 
            }
            break;
        case 's':
            if (!simulateDown) {
                console.log("Simulating inverter down...");
                simulateDown = true; 
            }
            break;
        case 'r':
            if (simulateDown) {
                console.log("Simulating inverter up again...");
                simulateDown = false; 
            }
            break;
        }
});

openSocket();
