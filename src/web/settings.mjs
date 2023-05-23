import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';
const __filename = fileURLToPath(import.meta.url);
export const __dirname = path.dirname(__filename);

export const binDir = path.join(__dirname, '../../target/bin');
export const etcDir = path.join(__dirname, '../../target/etc');

export const logsFile = path.join(etcDir, 'log.txt');
let serverCfgFile = path.join(etcDir, 'server/webCfg.json');

if (!fs.existsSync(serverCfgFile)) {
    fs.writeFileSync(serverCfgFile, JSON.stringify({
        username: "user",
        password: "pa$$word"
    }, null, 3));
}

export const settings = JSON.parse(fs.readFileSync(serverCfgFile, 'utf8'));

export const logger = (msg, echo) => {
    msg = new Date().toISOString() + " " + msg;
    fs.appendFileSync(logsFile, msg + '\n');
    if (echo) {
        console.log(msg);
    }
}
