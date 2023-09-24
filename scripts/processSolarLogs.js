import fs from "fs";
import path from "path";
import process from "process";
import readline from 'readline';

if (process.argv.length <= 2) {
    console.log("Usage <script> <sourceFolder>");
    process.exit(1);
}

/**
 * Aggregates solar daily log files
 */

const readCsv = async file => {
    const fileStream = fs.createReadStream(file);
    const rl = readline.createInterface({
        input: fileStream
    });

    let energyTodayWhIdx = -1;
    let totalEnergyKWhIdx = -1;
    let powerWIdx = -1;
    let lastEnergyToday = 0;
    let energyToday = 0;
    let totalEnergy = 0;
    let peak = 0;
    for await (const line of rl) {
        if (!line) continue;

        const parts = line.split(",");
        if (parts.length > 1) {
            const _energyToday = Number(parts[energyTodayWhIdx]);
            const _totalEnergy = Number(parts[totalEnergyKWhIdx]);
            const power = Number(parts[powerWIdx]);
            if (isNaN(_energyToday) || isNaN(_totalEnergy) || isNaN(power)) {
                const map = parts.reduce((map, it, idx) => {
                    map[it] = idx;
                    return map;
                }, { });
                if (!(energyTodayWhIdx = map["EnergyTodayWh"])) {
                    throw new Error(`EnergyTodayWh not found in ${file}`);
                }
                if (!(totalEnergyKWhIdx = map["TotalEnergyKWh"])) {
                    throw new Error(`TotalEnergyKWh not found in ${file}`);
                }
                if (!(powerWIdx = map["PowerW"])) {
                    throw new Error(`PowerW not found in ${file}`);
                }
            } else {
                if (_energyToday < lastEnergyToday) {
                    // Jumped due to darkness
                    energyToday += lastEnergyToday;
                }
                lastEnergyToday = _energyToday;
                totalEnergy = Math.max(totalEnergy, _totalEnergy);
                peak = Math.max(peak, power);
            }
        }
    }
    energyToday += lastEnergyToday;
    return { energyToday, totalEnergy, peak };
}

const writeDailyCsv = async map => {
    await fs.promises.writeFile("daily.csv", "Date,EnergyTodayWh,TotalEnergyKWh,Peak\n");
    await Object.keys(map).sort().reduce(async (lastPromise, date) => {
        await lastPromise;
        await fs.promises.appendFile("daily.csv", `${date},${map[date].energyToday},${map[date].totalEnergy},${map[date].peak}\n`);
    });
}

const aggregateYear = map => {
    const years = { };
    Object.keys(map).forEach(day => {
        const year = day.substring(0, 4);
        const yd = years[year] || (years[year] = { minTotalEnergy: Infinity, maxTotalEnergy: 0, peak: 0, peakDate: '' });
        if (map[day].totalEnergy) {
            yd.minTotalEnergy = Math.min(yd.minTotalEnergy, map[day].totalEnergy);
            yd.maxTotalEnergy = Math.max(yd.maxTotalEnergy, map[day].totalEnergy);
        }
        if (map[day].peak > yd.peak) {
            yd.peak = map[day].peak;
            yd.peakDate = day;
        }
    });
    return years;
};

const writeYearlyCsv = async map => {
    await fs.promises.writeFile("yearly.csv", "Year,TotalEnergyKWh,Peak,PeakDate\n");
    await Object.keys(map).sort().reduce(async (lastPromise, year) => {
        await lastPromise;
        await fs.promises.appendFile("yearly.csv", `${year},${map[year].maxTotalEnergy - map[year].minTotalEnergy},${map[year].peak},${map[year].peakDate}\n`);
    }, Promise.resolve());
}

const main = async (dir) => {
    const content = await fs.promises.readdir(dir);
    const files = content.filter(file => file.match(/[0-9]{4}-[0-9]{2}-[0-9]{2}\.csv/));
    console.log(`Processing ${files.length} files...`);

    const dailyMap = { };
    await Promise.all(files.map(async file => {
        dailyMap[file.replace(".csv", "")] = await readCsv(path.join(dir, file));
    }));

    await writeDailyCsv(dailyMap);
    const yearlyMap = aggregateYear(dailyMap);
    await writeYearlyCsv(yearlyMap);
};
main(process.argv[2]);