const child_process = require('child_process');
const path = require('path');
const net = require('net');
const { binDir, etcDir, logger } = require('./settings');

let restartMailText;
let process;
let killing;
const processName = 'Home.Server.exe';

/**
 * Manages home server health
 */
function start() {
    // Aready started
    if (process && process.pid) {
        throw new Error("Already started");
    }

    // Launch process
    let args = ['-wrk', etcDir];
    if (restartMailText) {
        args.push('-sendMail');
        args.push(restartMailText);
    }
    process = child_process.spawn(path.join(binDir, processName), args, {
        stdio: 'ignore'
    });
    restartMailText = null;

    process.once('exit', async (code, signal) => {
        process = null;
        if (code == 0xE0434352) {
            code = ".NetException";
        }
        if (!killing) {
            logger('Server process closed with code ' + code + ", signal " + signal, true);

            // Store fail reason to send mail after restart
            restartMailText = 'Server process closed with code ' + code + ", signal " + signal + '. Restarting';
            await new Promise(resolve => setTimeout(resolve, 3500));
            start();
        } else {
            logger('Server killed.', true);
        }
        killing = false;
    });

    process.on('err', (err) => {
        logger('Server process FAIL TO START: ' + err.message, true);
        process = null;
    });

    logger('Home server started.', true);
}

async function kill() {
    logger('Home server killing...', true);
    return new Promise((resolve, reject) => {
        // Aready started
        if (!process || !process.pid) {
            reject(new Error("Already killed"));
        }
        killing = true;
        process.once('exit', () => {
            resolve();
        });
        sendMessage({ command: "kill" });
    });
}

async function restart() {
    await kill();
    logger('Home server killed for restarting...', true);
    await new Promise(resolve => setTimeout(resolve, 3500));
    start();
}

function sendMessage(msgType, data) {
    if (typeof msgType !== "string") {
        data = msgType;
        msgType = null;
    }
    if (msgType) {
        // Polymorphism support for C# DataContractJsonSerializer requires __types to be the first property
        data = Object.assign({ __type: msgType + ":Net" }, data);
    }
    const message = JSON.stringify(data);
    return new Promise((resolve, reject) => {
        // Make request to server
        let pipe = net.connect('\\\\.\\pipe\\NETHOME', () => {
            // Connected
            pipe.setNoDelay(true);
            pipe.setDefaultEncoding('utf8');
            
            let resp = '';
    
            let respond = () => {
                pipe.destroy();
                let obj;
                try {
                    obj = JSON.parse(resp);
                }
                catch (err) {
                    obj = { exc: err.message };
                }
                resolve(obj);
            };
    
            pipe.on('data', data => {
                resp += data.toString();
                if (resp.charCodeAt(resp.length - 1) === 13) {
                    respond();
                }
            });
            pipe.once('end', () => {
                respond();
            });
            
            // Send request
            pipe.write(message + '\r\n');
        });
        pipe.on('error', err => reject(err));
    });
}

module.exports = { start, restart, sendMessage, kill };
