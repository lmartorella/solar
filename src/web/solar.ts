import * as fs from 'fs';
import * as path from 'path';
import * as moment from 'moment';
import { csvFolder } from './settings';

interface IPvImmData {
    error?: string;
    currentW?: number;
    currentTsTime?: string;
    currentTsDate?: string;
    totalDayWh?: number;
    peakW?: number;
    peakTs?: string;
    totalKwh?: number;
    mode?: number;
    fault?: string;
}

interface ICsv {
    rows: any[][];
    colKeys: { [key: string]: number };
}

function parseCsv(path: string): ICsv {
    var content = fs.readFileSync(path, 'utf8');
    var colKeys: { [key: string]: number } = { };
    var rows = content.split('\n').map((line, idx) => {
        var cells = line.replace('\r', '').split(',');
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
                    return (cell.indexOf(':') > 0) && cell;
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
}

function findPeak(csv: ICsv, colKey: string): any[] {
    var peakRow = csv.rows[0];
    var idx = csv.colKeys[colKey];
    csv.rows.forEach(row => {
        if (row[idx] > peakRow[idx]) {
            peakRow = row;
        }
    });
    return peakRow;
}

function decodeFault(fault: number): string {
    switch (fault) { 
        case 0x800:
            return "Mancanza rete";
        case 0x1000:
            return "Frequenza rete troppo bassa";
        case 0x2000:
            return "Frequenza rete troppo alta";
    }
    return fault && ('0x' + fault.toString(16));
}

// Day can be 0 or -1 (T-1), -2, etc..
function getCsvName(day?: number): string {
    // Get the latest CSV in the disk
    var files = fs.readdirSync(csvFolder).filter(f => fs.lstatSync(path.join(csvFolder, f)).isFile() && f[0] !== '_');
    // Sort it by date
    files = files.sort();
    var idx = files.length - 1 + (day || 0);

    if (idx < 0) {
        return null;
    }
    // Take the T-N one
    return files[idx];
}

function getPvData(): IPvImmData {
    var csv = getCsvName(0);
    if (!csv) {
        return { error: 'No files found' };
    }

    // Now parse it
    var data = parseCsv(path.join(csvFolder, csv));

    var ret: IPvImmData = { currentW: 0 };
    if (data.rows.length > 1) {
        var lastSample = data.rows[data.rows.length - 1];
        ret.currentW = lastSample[data.colKeys['PowerW']];
        ret.currentTsTime = lastSample[data.colKeys['TimeStamp']];
        ret.currentTsDate = csv.replace('.csv', ''); 
        ret.totalDayWh = lastSample[data.colKeys['EnergyTodayWh']]; 
        ret.totalKwh = lastSample[data.colKeys['TotalEnergyKWh']]; 
        ret.mode = lastSample[data.colKeys['Mode']];
        ret.fault = decodeFault(lastSample[data.colKeys['Fault']]);

        // Find the peak power
        var peakPow = findPeak(data, 'PowerW');
        ret.peakW = peakPow[data.colKeys['PowerW']];
        ret.peakTs = peakPow[data.colKeys['TimeStamp']];
    }
    return ret;
}

function formatDur(dur: moment.Duration): string {
    var ts = moment().startOf('day').add(dur);
    return ts.format('HH:mm');
}


// Sample each round minute
function sampleAtMin(arr: { ts: string, value: number }[]): { ts: string, value: number }[] {
    if (arr.length === 0) {
        return [];
    }

    let toDateMin = (str: string) => {
        var dur = moment.duration(str);
        return moment.duration(Math.floor(dur.asMinutes()), "minutes");
    }

    let lastMin: moment.Duration = toDateMin(arr[0].ts);
    let count = 0;
    let acc = 0;
    return arr.reduce((ret, val) => {
        acc += val.value;
        count++;
        let min = toDateMin(val.ts);
        if (min > lastMin) {
            ret.push({ ts: formatDur(min), value: acc / count });
            count = 0;
            acc = 0;
            lastMin = min;
        }   
        return ret;     
    }, []);
}

function first<T>(arr: T[], handler: (t: T) => boolean): number {
    for (let i = 0; i < arr.length; i++) {
        if (handler(arr[i])) {
            return i;
        }
    }
    return -1;
}

function last<T>(arr: T[], handler: (t: T) => boolean): number {
    for (let i = arr.length - 1; i >= 0; i--) {
        if (handler(arr[i])) {
            return i;
        }
    }
    return -1;
}

function getPvChart(day?: number): { ts: string, value: number }[] {
    var csv = getCsvName(day);
    if (!csv) {
        return [];
    }
    var data = parseCsv(path.join(csvFolder, csv));
    var tsIdx = data.colKeys['TimeStamp'];
    var powIdx = data.colKeys['PowerW'];
    let ret = sampleAtMin(data.rows.map(row => {
        return { ts: row[tsIdx] as string, value: row[powIdx] as number };
    }));
    // Trim initial and final zeroes
    let i1 = first(ret, i => i.value > 0);
    let i2 = last(ret, i => i.value > 0);
    if (i1 >= 0 && i2 >= 0) {
        return ret.slice(i1, i2);
    } else {
        return [];
    }
}

export { getPvData, getPvChart };