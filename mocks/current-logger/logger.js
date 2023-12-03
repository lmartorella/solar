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

// Do a scan every 5s
const period = 5000;

const asleep = t => new Promise(resolve => setTimeout(() => resolve(), t));

process.on('SIGINT', async () => {
    console.log("Caught interrupt signal");
    process.exit(1);
});

const client = new Modbus.client.TCP(socket, 1);
const v = 230;
const maxA = 50;

/**
 * @returns homeFrac?, prodFrac?, homeW?, prodW?, err?, errCode?
 */
const doRead = async () => {
    try {
        const resp = await client.readHoldingRegisters(0x200, 4);
        // In case of OK read, wait
        await asleep(period);
        const buffer = resp.response.body.valuesAsBuffer;
        const homeFrac = buffer.readUint16LE(0);
        const prodFrac = buffer.readUint16LE(4);
        const home = buffer.readUint32LE(0);
        const prod = buffer.readUint32LE(4);
        return { homeA: (home / 65536.0 / 1024.0 * maxA * v).toFixed(1), prodA: (prod / 65536.0 / 1024.0 * maxA * v).toFixed(1), homeFrac, prodFrac };
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
        return { err: err.err, errCode: err.response?.body?.code };
    }
}

socket.on("connect", async () => {
    console.log("Ts,HomeW,ProdW,HomeFrac,ProdFrac,Err,ErrCode");
    while (true) {
        const ts = new Date().toLocaleTimeString();
        const data = await doRead();
        console.log(`${ts},${data.homeA || ""},${data.prodA || ""},${data.homeFrac || ""},${data.prodFrac || ""},${data.err || ""},${data.errCode || ""}`);
    }
});

socket.connect({ host, port: 502, keepAlive: true });