import * as fs from 'fs';
import * as path from 'path';

let binDir = path.join(__dirname, 'bin');
let etcDir = path.join(__dirname, 'etc');

let csvFolder = path.join(etcDir, 'DB', 'SAMIL');
if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
    throw new Error('CSV folder not accessible: ' + csvFolder);
}

let logsFile = path.join(etcDir, 'log.txt');
let serverCfgFile = path.join(etcDir, 'Server', 'config.json');
let gardenCfgFile = path.join(etcDir, 'Server', 'gardenCfg.json');
let gardenCsvFile = path.join(etcDir, 'DB', 'GARDEN', 'garden.csv');

export { csvFolder, binDir, etcDir, logsFile, serverCfgFile, gardenCfgFile, gardenCsvFile };

