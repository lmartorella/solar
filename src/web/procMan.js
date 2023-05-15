const child_process = require('child_process');
const path = require('path');
const { binDir, etcDir, logger } = require('./settings');
const { remoteCall } = require('./mqtt');

let restartMailText;
let process;
let killing;
const processName = 'Home.Server.exe';

/**
 * Manages home server health
 */
const start = () => {
    // Already started
    if (process && process.pid) {
        throw new Error("Already started");
    }

    // Launch process
    let args = ['-wrk', etcDir];
    if (restartMailText) {
        args.push('-sendMailErr');
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
};

const kill = () => {
    logger('Home server killing...', true);
    return new Promise((resolve, reject) => {
        // Already started
        if (!process || !process.pid) {
            reject(new Error("Already killed"));
        }
        killing = true;
        process.once('exit', () => {
            resolve();
        });
        remoteCall("kill");
    });
};

const restart = async () => {
    await kill();
    logger('Home server killed for restarting...', true);
    await new Promise(resolve => setTimeout(resolve, 3500));
    start();
};


module.exports = { start, restart, kill };
