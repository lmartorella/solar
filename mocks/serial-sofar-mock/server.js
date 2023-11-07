import { SerialPort } from 'serialport';
import { InterByteTimeoutParser } from '@serialport/parser-inter-byte-timeout';
import crc16modbus from 'crc/crc16modbus';

// Ratio of packet loss, that simulates timeout issues
const TimeoutRatio = 0.15;
// Ratio of error 0x90
const ErrorRatio = 0.15;
// Ratio of splitted packet, with more than 3.5 spaces in between bursts
const SplitMessageRatio = 0.15;

const openPort = async () => {
    const ports = (await SerialPort.list()).filter(port => port.friendlyName.indexOf("USB-to-Serial Comm Port") >= 0);
    if (ports.length === 0) {
        throw new Error("No COM ports found with the name `USB-to-Serial Comm Port`");
    }

    const port = new SerialPort({
        path: ports[0].path,
        baudRate: 9600,
    });

    return new Promise((resolve, reject) => {
        // The open event is always emitted
        port.on('open', err => {
            if (err) {
                reject(err);
            } else {
                resolve(port);
            }
        });
    });
}

const toHex = buffer => {
    return Array.from(buffer).map(byte => `[${byte.toString(16).padStart(2, '0')}]`).join('');
};

(async () => {
    const port = await openPort();

    // Use RTS-based RS485 adapter. RTS low = transmit
    port.set({ rts: true });

    const parser = port.pipe(new InterByteTimeoutParser({ interval: 1 })); // in ms
    const inverterData = {
        start: 1024,
        data: Array.from({ length: 2048 }, () => Math.floor(Math.random() * 65535))
    };

    const writeData = async (resp) => {
        const splitMessage = Math.random() < SplitMessageRatio;
        let packets = [resp];
        if (splitMessage) {
            // Simulate splitted message
            console.log(" splitted");
            const l = Math.floor(resp.length / 2);
            packets = [resp.slice(0, l), resp.slice(l)];
        }

        port.set({ rts: false });
        // Sofar writes immediately

        for (const packet of packets) {
            console.log("-> ", toHex(packet));
            port.write(packet);
            await new Promise((resolve, reject) => {
                port.drain(err => {
                    if (err) reject(err); else resolve();
                });
            })
            // Wait for complete flush + 1ms
            // TODO: Not sure what is the granularity on the OS (it can be 18ms on Windows)
            let timeout = 1 + (10 * packet.length) / 9600 * 1000;
            if (splitMessage) {
                timeout += 6;
            }
            await new Promise(resolve => setTimeout(resolve, timeout));
        }

        port.set({ rts: true });
    }

    // Strange non-MODBUS response, with a CRC of 0, a fn code of 0x90 but a "real" 0x2 error in case
    // an invalid address is requested
    const respondWithError = () => {
        const resp = new Uint8Array([0x01, 0x90, 0x02, 0x00, 0x00]); // 0x2: Illegal Data Address
        return writeData(resp);
    };

    parser.on('data', async data => {
        console.log("<- ", toHex(data));

        if (Math.random() < TimeoutRatio) {
            // Simulate packet loss
            console.log(" dropped");
            return;
        }

        if (data.length === 9 && data[8] === 0) {
            // That's ok, skip the last
        } else if (data.length !== 8) {
            console.log(" Skip: invalid length");
            return;
        }

        if (data[0] != 1) {
            console.log(" Skip: not addressed to node 1");
            return;
        }
        const crc = (data[7] << 8) + data[6];
        if (crc16modbus(data.slice(0, 6)) !== crc) {
            console.log(" Skip: invalid CRC");
            return;
        }

        // Decode the function code
        if (Math.random() < ErrorRatio) {
            console.log(" Simulate random error");
            await respondWithError();
            return;
        }
        
        // Decode the function code
        if (data[1] != 3) {
            console.log(" Err: function code expected: 0x03");
            await respondWithError();
            return;
        }
        // Decode the address init
        const regAddr = (data[2] << 8) + data[3];
        const regCount = (data[4] << 8) + data[5];
        if (regAddr < inverterData.start || regCount < 1 || regAddr + regCount > inverterData.start + inverterData.data.length) {
            console.log(" Err: invalid range requested");
            await respondWithError();
            return;
        }

        // Ok, valid range, respond
        const resp = new Uint8Array(2 + 1 + regCount * 2 + 2);
        resp[0] = 1;
        resp[1] = 3;
        resp[2] = regCount * 2;
        let idx = 3;
        for (let i = 0; i < regCount; i++) {
            const regData = inverterData.data[i + regAddr - inverterData.start];
            resp[idx++] = regData >> 8;
            resp[idx++] = regData & 0xff;
        }
        const respCrc = crc16modbus(resp.slice(0, idx));
        resp[idx++] = respCrc & 0xff;
        resp[idx++] = respCrc >> 8;

        // Respond
        return writeData(resp);
    });
})();