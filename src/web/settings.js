const path = require('path');
const fs = require('fs');

let binDir = path.join(__dirname, '../../target/bin');
let etcDir = path.join(__dirname, '../../target/etc');

let logsFile = path.join(etcDir, 'log.txt');
let serverCfgFile = path.join(etcDir, 'server/webCfg.json');

if (!fs.existsSync(serverCfgFile)) {
    fs.writeFileSync(serverCfgFile, JSON.stringify({
        username: "user",
        password: "pa$$word"
    }, null, 3));
}

const settings = JSON.parse(fs.readFileSync(serverCfgFile, 'utf8'));

const logger = (msg, echo) => {
    msg = new Date().toISOString() + " " + msg;
    fs.appendFileSync(logsFile, msg + '\n');
    if (echo) {
        console.log(msg);
    }
}

module.exports = { binDir, etcDir, logsFile, settings, logger };

