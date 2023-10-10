import fs from 'fs';
import Modbus from 'jsmodbus';
import net from 'net';
import process from 'process';

const socket = new net.Socket();

if (process.argv.length < 3) {
    console.error("Expected hostname");
    process.exit(1);
}
const host = process.argv[2];

// Read 8 registers at a time. Too broad windows could lead to "address errors" for valid registers, who knows.
const maxRangeWindow = 10;
// Do a scan every 500ms
const period = 1000;
// Timeout to abandon the query
const timeout = 750;

const parseHex = s => {
    return parseInt(s.replace("0x", ""), 16);
}

const annotationData = JSON.parse(fs.readFileSync("annotation.json"));
const annotations = []; // sparse array
const addresses = Object.keys(annotationData.registries).filter(r => r[0] !== "!").map(key => {
    const address = parseHex(key);
    annotations[address] = annotationData.registries[key];
    return address;
});

const ranges = [];
let firstAddr = addresses[0];
let lastAddr = addresses[0];
for (const address of addresses) {
    if (address > firstAddr + maxRangeWindow) {
        // Split reg
        ranges.push({ from: firstAddr, to: lastAddr });
        firstAddr = address;
    }
    lastAddr = address;
}
if (firstAddr !== lastAddr) {
    ranges.push({ from: firstAddr, to: lastAddr });
}

const asleep = t => new Promise(resolve => setTimeout(() => resolve(), t));

const doScan = async (address, len) => {
    try {
        const resp = await client.readHoldingRegisters(address, len);
        // In case of OK read, wait
        await asleep(period);
        return { data: resp.response.body.valuesAsArray };
    } catch (err) {
        if (err.err !== "Timeout" && err.err !== "ModbusException") {
            throw err;
        }
        if (err.err === "Timeout") {
            if (period > timeout) {
                await asleep(period - timeout);
            }
        } else {
            await asleep(period);
        }
        return { timeout: err.err === "Timeout", invalidAddress: err.response?.body?.code === 2, errCode: err.response?.body?.code };
    }
}

process.on('SIGINT', async () => {
    console.log("\nCaught interrupt signal");
    process.exit(1);
});

const toHex = n => {
    return "0x" + Number(n).toString(16);
};

const client = new Modbus.client.TCP(socket, 1, timeout);
socket.on("connect", async () => {
    const lastValues = [];
    while (true) {
        await ranges.reduce(async (lastP, range) => {
            await lastP;
            const len = range.to - range.from + 1;
            const result = await doScan(range.from, len);
            if (result.data) {
                for (let i = 0; i < len; i++) {
                    const reg = range.from + i;
                    lastValues[reg] = result.data[i];
                    const annotation = annotations[reg];
                    if (annotation) {
                        console.log(`[${toHex(reg)}] ${result.data[i]}: ${annotation}`);
                    }
                }
            }
        }, Promise.resolve());
    }
});

socket.connect({ host, port: 502, keepAlive: true });