import { SerialPort } from 'serialport';
import { InterByteTimeoutParser } from '@serialport/parser-inter-byte-timeout';
import crc16modbus from 'crc/crc16modbus';

const openPort = async () => {
    const ports = (await SerialPort.list()).filter(port => port.friendlyName.indexOf("USB-to-Serial Comm Port") >= 0);

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
    const inverterData = (await import("./data.json", { assert: { type: "json" } })).default;

    const writeData = async (resp) => {
        console.log("-> ", toHex(resp));
        port.set({ rts: false });
        await new Promise(resolve => setTimeout(resolve, 1));
        port.write(resp);
        // Wait for complete flush + 1ms
        await new Promise(resolve => setTimeout(resolve, 1 + (10 * resp.length) / 9600 * 1000));
        port.set({ rts: true });
    }

    // Strange non-MODBUS response
    const respondWithError = () => {
        const resp = new Uint8Array([0x01, 0x90, 0x02, 0x00, 0x00]);
        return writeData(resp);
    };

    parser.on('data', async data => {

        console.log("<- ", toHex(data));

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
        const resp = new Uint8Array(regCount * 2 + 4 + 2);
        resp[0] = 1;
        resp[1] = 3;
        resp[2] = data[2];
        resp[3] = data[3];
        let idx = 4;
        for (let i = 0; i < regCount; i++) {
            resp[idx++] = inverterData.data[i + inverterData.start] >> 8;
            resp[idx++] = inverterData.data[i + inverterData.start] & 0xff;
        }
        const respCrc = crc16modbus(resp.slice(0, idx));
        resp[idx++] = respCrc & 0xff;
        resp[idx++] = respCrc >> 8;

        // Respond
        return writeData(resp);
    });
})();