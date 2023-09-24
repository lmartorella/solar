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
const rangeWindow = 8;
// Scan only the range between 0x0 and 0x2000
const range = [0, 0x2000];
// Do a scan every 500ms
const period = 1000;
// Timeout to abandon the query
const timeout = 750;

const load = () => {
    if (fs.existsSync("dump.json")) {
        return JSON.parse((fs.readFileSync("dump.json")).toString());
    } else {
        return { scans: { } };
    }
};

const save = async () => {
    await fs.promises.writeFile("dump.json", JSON.stringify({
        scans,
        rangeWindow
    }));
};

// By register address
// Once an address has received at least a valid "invalid register", it is marked as dead.
const { scans } = load();

const asleep = t => new Promise(resolve => setTimeout(() => resolve(), t));

const doScan = async (address) => {
    try {
        const resp = await client.readHoldingRegisters(address, rangeWindow);
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
    console.log("Caught interrupt signal");
    await save();
    process.exit(1);
});

const client = new Modbus.client.TCP(socket, 1, timeout);
socket.on("connect", async () => {
    await load();
    let currentAddress = range[0];

    while (true) {
        let overrun = false;
        while (scans[currentAddress.toString()]?.isInvalid) {
            currentAddress += rangeWindow;
            if (currentAddress >= range[1]) {
                console.log("Restart the range");
                await save();
                if (overrun) {
                    console.log("Everything seems empty");
                    process.exit(0);
                }
                currentAddress = range[0];
                overrun = true;
            }
        }

        const key = currentAddress.toString();
        scans[key] = scans[key] || { data: [] };
        const result = await doScan(currentAddress);

        if (result.invalidAddress) {
            if (scans[key].data.filter(sample => !!sample.data).length > 0) {
                // Strange, received invalid address for something that was actually data in a previous cycle. Log it but doesn't discard it
                scans[key].data.push({ ts: new Date(), error: "InvalidAddress" });
            } else {
                scans[key].data = [];
                scans[key].isInvalid = true;
            }
            process.stdout.write("X");
        } else if (result.data) {
            scans[key].data.push({ ts: new Date(), data: result.data });
            process.stdout.write("O");
        } else if (result.timeout) {
            process.stdout.write(".");
        } else {
            // Strange, received modbus error. Log it
            scans[key].data.push({ ts: new Date(), error: `code ${result.errCode}` });
            process.stdout.write(`?${result.errCode}?`);
        }
    
        // Pick next range
        currentAddress += rangeWindow;
    }
});

socket.connect({ host, port: 502, keepAlive: true });