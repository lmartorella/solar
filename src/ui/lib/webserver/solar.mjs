import path from "path";
import fs from "fs";
import moment from "moment";
import { setTimeout } from "timers";
import { subscribeJsonTopic } from "./mqtt.mjs";

let csvFolder;

const parseCsv = path => {
    const content = fs.readFileSync(path, "utf8");
    let colKeys = { };
    const rows = content.split("\n").map((line, idx) => {
        const cells = line.replace("\r", "").split(",");
        if (idx === 0) {
            // Decode keys
            colKeys = cells.reduce((acc, k, i) => {
                acc[k] = i;
                return acc;
            }, colKeys);
            return null;
        } else {
            return cells.map((cell, i) => {
                if (i === 0) {
                    // First column should be a time. If not, nullify the whole row (e.g. csv headers)
                    return (cell.indexOf(":") > 0) && cell;
                } else {
                    // Other columns are number
                    return Number(cell);
                }
            });
        }
    }).filter(row => row && row[0]);
    return {
        colKeys,
        rows
    };
};

/**
 * @param day can be a string in the yyyy-mm-dd format
 * @param deltaDay is a negative number to apply to today. E.g. -1 means yesterday data, etc...
 * If not passed, it means today
 */
const getCsvName = ({ day, deltaDay }) => {
    if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
        return null;
    }

    if (day) {
        return fs.existsSync(path.join(csvFolder, `${day}.csv`)) && `${day}.csv`;
    } else {
        // Get the latest CSV in the disk
        let files = fs.readdirSync(csvFolder).filter(f => fs.lstatSync(path.join(csvFolder, f)).isFile() && f[0] !== "_");
        // Sort it by date
        files = files.sort();
        const idx = files.length - 1 + (deltaDay || 0);

        if (idx < 0) {
            return null;
        }
        // Take the T-N one
        return files[idx];
    }
};

function formatDur(dur) {
    const ts = moment().startOf("day").add(dur);
    return ts.format("HH:mm");
}

// Sample each round minute
function averageToMinute(arr, props) {
    if (arr.length === 0) {
        return [];
    }

    const toDateMin = (str) => {
        const dur = moment.duration(str);
        return moment.duration(Math.floor(dur.asMinutes()), "minutes");
    };

    let lastMin = toDateMin(arr[0].ts);
    let count = 0;

    const accumulators = { };
    props.forEach(prop => accumulators[prop] = 0);

    return arr.reduce((ret, val) => {
        props.forEach(prop => accumulators[prop] += val[prop]);
        count++;
        const min = toDateMin(val.ts);
        if (min > lastMin) {
            const sample = { ts: formatDur(min) };
            props.forEach(prop => sample[prop] = accumulators[prop] / count);
            ret.push(sample);
            count = 0;
            props.forEach(prop => accumulators[prop] = 0);
            lastMin = min;
        }   
        return ret;
    }, []);
}

function first(arr, handler) {
    for (let i = 0; i < arr.length; i++) {
        if (handler(arr[i])) {
            return i;
        }
    }
    return -1;
}

function last(arr, handler) {
    for (let i = arr.length - 1; i >= 0; i--) {
        if (handler(arr[i])) {
            return i;
        }
    }
    return -1;
}

/**
 * @param day can be a string in the yyyy-mm-dd format
 * @param deltaDay is a negative number to apply to today. E.g. -1 means yesterday data, etc...
 * If not passed, it means today
 */
const getPvChart = ({ day, deltaDay }) => {
    const csv = getCsvName({ day, deltaDay });
    if (!csv) {
        return [];
    }
    const data = parseCsv(path.join(csvFolder, csv));
    const tsIdx = data.colKeys["TimeStamp"];
    const powIdx = data.colKeys["PowerW"];
    const voltageIdx = data.colKeys["GridVoltageV"];

    const ret = averageToMinute(data.rows.map(row => {
        return { ts: row[tsIdx], power: row[powIdx], voltage: row[voltageIdx] };
    }), ["power", "voltage"]);

    // Trim initial and final zeroes
    const i1 = first(ret, i => i.power > 0);
    const i2 = last(ret, i => i.power > 0);
    if (i1 >= 0 && i2 >= 0) {
        return ret.slice(i1, i2);
    } else {
        return [];
    }
};

let lastData = null;
let lastErr;

export function register(app, _csvFolder) {
    csvFolder = _csvFolder;

    subscribeJsonTopic("ui/solar", data => (lastData = data, lastErr = null), err => (lastErr = err, lastData = null));

    app.get("/solar/solarStatus", async (_req, res) => {
        if (lastErr) {
            res.statusMessage = lastErr.message;
            res.status(500).end();
        } else {
            res.send(lastData);
        }
    });

    app.get("/solar/solarPowToday", (req, res) => {
        // Avoid DDOS
        setTimeout(() => {
            const args = {
                // if passed, day is a string in yyyy-mm-dd format
                day: req.query?.day,
                // deltaDay is a negative number to apply to today. E.g. -1 means yesterday data, etc...
                deltaDay: Number(req.query?.deltaDay)
            };
            // If nothing is passed, it means today
            res.send(getPvChart(args));
        }, 250);
    });
}
