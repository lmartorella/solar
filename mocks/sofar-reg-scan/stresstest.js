import Modbus from 'jsmodbus';
import net from 'net';
import process from 'process';

const socket = new net.Socket();

const showCliErr = () => {
    console.error(
`Expected hostname and testcode

Test codes:
    1. Test fast bursts of a registry range
`);
    process.exit(1);
};

if (process.argv.length < 4) {
    showCliErr();
}

// Timeout to abandon the query
const TIMEOUT = 250;

const host = process.argv[2];
const testCode = Number(process.argv[3]);

let calls = 0;
let successfulCalls = 0;

const asleep = t => new Promise(resolve => setTimeout(() => resolve(), t));
const test1 = async () => {
    const BURST_COUNT = 10;
    const BASE_ADDRESS = 0x484;
    const END_ADDRESS = 0x48e;  // grid data
    while (true) {
        const results = [];
        try {
            for (let n = 0; n < BURST_COUNT; n++) {
                // Without any sleep in the middle
                try {
                    calls++;
                    await client.readHoldingRegisters(BASE_ADDRESS, END_ADDRESS - BASE_ADDRESS + 1);
                    successfulCalls++;
                    // Ok
                    results.push("O");
                } catch (err) {
                    if (err.err === "Timeout") {
                        // Timeout
                        results.push(".");
                    } else if (err.err === "ModbusException") {
                        // Modbus error
                        results.push("E");
                    } else {
                        throw err;
                    }
                }
            }
        } finally {
            console.log(results.join(""));
        }
        await asleep(TIMEOUT);
    }
}

process.on('SIGINT', async () => {
    console.log("Caught interrupt signal");
    console.log(`Success ratio ${(successfulCalls / calls * 100).toFixed(1)}%`);
    process.exit(0);
});

const main = async () => {
    try {
        switch (testCode) {
            case 1:
                await test1();
                break;
            default:
                showCliErr();
        }
    } catch (err) {
        console.error(`EXC: ${err.err ||  "<Unknown>"} ${err.message}`);
        process.exit(1);
    }
};

const client = new Modbus.client.TCP(socket, 1, TIMEOUT);
socket.on("connect", () => main());

socket.connect({ host, port: 502, keepAlive: true });