import * as child_process from 'child_process';
import * as path from 'path';
import * as fs from 'fs';
import * as net from 'net';

/**
 * Manages home server health
 */
export default class ProcessManager {
    process: child_process.ChildProcess;
    logPath: string;
    killing: boolean;
    restartMailText: string;

    constructor(private binPath: string, private etcPath: string, private processName: string) {
        this.logPath = path.join(this.etcPath, 'log.txt');
    }

    public start(): void {
        // Aready started
        if (this.process && this.process.pid) {
            throw new Error("Already started");
        }

        // Launch process
        let args = ['', '-wrk', this.etcPath];
        if (this.restartMailText) {
            args.push('-sendMail');
            args.push(this.restartMailText);
        }
        this.process = child_process.spawn(path.join(this.binPath, this.processName), args, {
            stdio: 'ignore'
        });
        this.restartMailText = null;

        this.process.once('exit', async (code: number, signal: string) => {
            this.log('Server process closed with code ' + code + ", signal " + signal);
            this.process = null;
            if (!this.killing) {
                // Store fail reason to send mail after restart
                this.restartMailText = 'Server process closed with code ' + code + ", signal " + signal + '. Restarting';

                await new Promise(resolve => setTimeout(resolve, 3500))
                this.start();
            }
        });

        this.process.on('err', (err) => {
            this.log('Server process FAIL TO START: ' + err.message);
            this.process = null;
        });

        console.log('Home server started.');
    }

    private log(msg: string): void {
        fs.appendFileSync(this.logPath, msg + '\n');
    }

    private async kill(): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            // Aready started
            if (!this.process || !this.process.pid) {
                reject(new Error("Already killed"));
            }
            this.killing = true;

            this.process.once('exit', () => {
                this.process = null;
                resolve();
            });
            resolve(this.sendMessage({ command: "kill" }));
        });
    }

    public async halt(): Promise<void> {
        await this.kill();
        console.log('Home server halted.');
    }

    public async restart(): Promise<void> {
        await this.kill();
        await new Promise(resolve => setTimeout(resolve, 3500))
        console.log('Home server killed. Restarting...');
        this.start();
    }

    public sendMessage(data: any): Promise<any> {
        return new Promise<string>((resolve, reject) => {
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
}
