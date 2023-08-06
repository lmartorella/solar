import fs from 'fs';

const load = () => {
    if (fs.existsSync("dump.json")) {
        return JSON.parse((fs.readFileSync("dump.json")).toString());
    } else {
        return {};
    }
};

const { scans, rangeWindow } = load();
const keys = Object.keys(scans).sort((a, b) => Number(a) - Number(b));

const toHex = n => {
    return "0x" + Number(n).toString(16);
}

// Blank-out invalid ranges
const validRanges = [];
let lastIsInvalid = !!scans[keys[0]].isInvalid;
let firstKey = keys[0]; // inclusive
for (const key of keys) {
    const isInvalid = !!scans[key].isInvalid;
    if (isInvalid !== lastIsInvalid) {
        if (!lastIsInvalid) {
            validRanges.push({ from: Number(firstKey), to: Number(key) });
        }
        firstKey = key;
    }
    lastIsInvalid = isInvalid;
}
if (!lastIsInvalid) {
    const lastKey = Number(keys[keys.length - 1]) + rangeWindow; // exclusive
    validRanges.push({ from: Number(firstKey), to: Number(lastKey) });
}

const fixedValues = { };
const movingValues = { };
validRanges.forEach(range => {
    for (let startAddress = range.from; startAddress < range.to; startAddress += rangeWindow) {
        const data = scans[startAddress.toString()].data.filter(sample => !!sample.data);
        if (!data.length) {
            // No samples
            continue;
        }
        for (let i = 0; i < rangeWindow; i++) {
            // Check if fixed number
            const samples = data.map(sample => sample.data[i]);
            const firstSample = samples[0];
            if (samples.every(v => v === firstSample)) {
                fixedValues[toHex(startAddress + i)] = firstSample;
            } else {
                movingValues[toHex(startAddress + i)] = samples;
            }
        }
    }
});

fs.writeFileSync("fixedValues.json", JSON.stringify(fixedValues, null, 2));
fs.writeFileSync("movingValues.json", JSON.stringify(movingValues, null, 2));
