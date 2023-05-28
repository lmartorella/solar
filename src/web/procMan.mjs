import child_process from 'child_process';
import path from 'path';
import { binDir, etcDir, logger } from './settings.mjs';
import { rawRemoteCall } from './mqtt.mjs';

/**
 * Manages process health
 */
export class ManagedProcess {
    constructor(processName, topic) {
        this.processName = processName;
        this.topic = topic;
        this.logFile = path.join(etcDir, `${this.processName}.log`)
    }

    _start() {
        // Already started
        if (this.process && this.process.pid) {
            throw new Error(`Server process ${this.processName} already started`);
        }

        // Launch process
        const args = ['-wrk', etcDir];
        if (this.restartMailText && ManagedProcess.enableMail) {
            args.push('-sendMailErr');
            args.push(this.restartMailText);
        }
        this.process = child_process.spawn(path.join(binDir, `${this.processName}.exe`), args, {
            stdio: 'ignore'
        });

        this.process.once('exit', async (code, signal) => {
            this.process = null;
            if (code == 0xE0434352) {
                code = ".NetException";
            }
            if (!this.killing) {
                const msg = `Server process ${this.processName} closed with code ${code}, signal ${signal}`;
                logger(msg, true);

                // Store fail reason to send mail after restart
                this.restartMailText = `${msg}. Restarting`;
                await new Promise(resolve => setTimeout(resolve, 3500));
                this._start();
            } else {
                logger(`Server process ${this.processName} killed`);
            }
            this.killing = false;
        });

        this.process.on('err', err => {
            logger(`Server process ${this.processName} FAIL TO START: ${err.message}`);
            this.process = null;
        });

        logger(`Home server ${this.processName} started`);
    }

    start(res) {
        try {
            this._start();
            res.send(`${this.processName} started`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }

    async _kill() {
        logger(`Server process ${this.processName} killing...`);
        await new Promise((resolve, reject) => {
            // Already started
            if (!this.process || !this.process.pid) {
                throw new Error(`Server process ${this.processName} killed`);
            }
            this.killing = true;
            this.process.once('exit', () => {
                resolve();
            });
            rawRemoteCall(`${this.topic}/kill`).catch(err => reject(err));
        });
    };

    async kill(res) {
        try {
            await this._kill();
            res.send(`${this.processName} killed`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }

    async _restart() {
        await this.kill(res);
        logger(`Server process ${this.processName} killed for restarting...`);
        await new Promise(resolve => setTimeout(resolve, 3500));
        this._start();
    };

    async restart(res) {
        try {
            await this._restart();
            res.send(`${this.processName} restarted`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }
}
