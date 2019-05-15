const child_process = require('child_process');
const path = require('path');
const fs = require('fs');
const net = require('net');

let logPath;
let restartMailText;
let process;
let killing;

/**
 * Manages home server health
 */
function start(binPath, etcPath, processName) {
    logPath = path.join(etcPath, 'log.txt');

    // Aready started
    if (process && process.pid) {
        throw new Error("Already started");
    }

    // Launch process
    let args = ['', '-wrk', etcPath];
    if (restartMailText) {
        args.push('-sendMail');
        args.push(restartMailText);
    }
    process = child_process.spawn(path.join(binPath, processName), args, {
        stdio: 'ignore'
    });
    restartMailText = null;

    process.once('exit', async (code, signal) => {
        log('Server process closed with code ' + code + ", signal " + signal);
        process = null;
        if (!killing) {
            // Store fail reason to send mail after restart
            restartMailText = 'Server process closed with code ' + code + ", signal " + signal + '. Restarting';

            await new Promise(resolve => setTimeout(resolve, 3500))
            start();
        }
    });

    process.on('err', (err) => {
        log('Server process FAIL TO START: ' + err.message);
        process = null;
    });

    console.log('Home server started.');
}

function log(msg) {
    fs.appendFileSync(logPath, msg + '\n');
}

async function kill() {
    return new Promise((resolve, reject) => {
        // Aready started
        if (!process || !process.pid) {
            reject(new Error("Already killed"));
        }
        killing = true;

        process.once('exit', () => {
            process = null;
            resolve();
        });
        resolve(sendMessage({ command: "kill" }));
    });
}

async function halt() {
    await kill();
    console.log('Home server halted.');
}

async function restart() {
    await kill();
    await new Promise(resolve => setTimeout(resolve, 3500));
    console.log('Home server killed. Restarting...');
    start();
}

function sendMessage(data) {
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
            pipe.once('end', data => {
                respond();
            });
            
            // Send request
            pipe.write(JSON.stringify(data) + '\r\n');
        });
        pipe.on('error', err => reject(err));
    });
}

module.exports = { start, halt, restart, sendMessage };
