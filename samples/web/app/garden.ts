import { res, format } from "./resources";
import { IHttpPromise } from "angular";

declare var moment: any;
moment.locale("it-IT");

interface IConfig {
    zones?: string[];
    program?: {
        cycles?: ICycle[];
    }
}

interface ICycle {
    name: string;
    start: string; // ISO format
    startTime: string; // HH:mm:ss format
    suspended: boolean;
    disabled: boolean;
    minutes: number;
}

interface IScheduledCycle extends ICycle {
    scheduledTime: string;
}

interface IGardenResponse {
    error?: string;
}

interface IGardenStatusResponse extends IGardenResponse {
    config: IConfig;
    online: boolean;
    isRunning: boolean;
    flowData: { 
        totalMc: number;
        flowLMin: number;
    };
    nextCycles: IScheduledCycle[];
}

interface IGardenStartStopResponse extends IGardenResponse {
    error: string;
}

class ImmediateCycle {
    constructor(zoneNames: string[], public time: string) {
        this.zones = zoneNames.map((name, index) => ({ name, index }))
    }

    zones: { 
        name: string;
        enabled?: boolean;
        index: number;
    }[];
}

export class GardenController {
    public loaded: boolean;
    public message: string;
    public error: string;
    private zoneNames: string[] = [];
    public immediateCycle: ImmediateCycle;
    public config: IConfig;
    public status: string;
    public flow: { 
        totalMc: number;
        flowLMin: number;
    };
    public nextCycles: { name: string, scheduledTime: string, suspended: boolean }[];
    public immediateStarted: boolean;
    public canSuspendAll: boolean;
    public canResumeAll: boolean;
    public editProgramMode: boolean;
    public isRunning: boolean;
    // To anticipate login request at beginning of an operation flow
    private _hasPrivilege: boolean;

    static $inject = ['$http', '$q'];
    constructor(private $http: ng.IHttpService, private $q: ng.IQService) { }

    public $onInit() {
        this.status = res["Device_StatusLoading"];
        this.loaded = false;
        this.loadConfigAndStatus();
    }

    private checkXhr<T extends IGardenResponse>(xhr: ng.IHttpPromise<T>): ng.IPromise<T> {
        return xhr.then(resp => {
            if (resp.status == 200 && resp.data) {
                if (resp.data.error) {
                    throw new Error(resp.data.error);
                } else {
                    return resp.data;
                }
            } else {
                throw new Error(resp.statusText);
            }
        }, err => {
            throw new Error(err.statusText || err.message);
        });
    }

    private preCheckPrivilege(): ng.IPromise<void> {
        if (this._hasPrivilege) {
            return this.$q.when();
        } else {
            return this.checkXhr(this.$http.get("/checkLogin")).then(() => {
                this._hasPrivilege = true;
            });
        }
    }

    private loadConfigAndStatus() {
        // Fetch zones
        this.checkXhr(this.$http.get<IGardenStatusResponse>("/svc/gardenStatus")).then(resp => {
            this.status =  resp.online ? res["Device_StatusOnline"] : (resp.config ? res["Device_StatusOffline"] : res["Garden_MissingConf"]);
            this.flow = resp.flowData;
            this.isRunning = resp.isRunning;

            let now = moment.now();
            if (resp.nextCycles) {
                this.nextCycles = resp.nextCycles;
                this.nextCycles.forEach(cycle => {
                    cycle.scheduledTime = cycle.scheduledTime && moment.duration(moment(cycle.scheduledTime).diff(now)).humanize(true)
                })
            }

            this.config = resp.config || { };
            this.zoneNames = this.config.zones = this.config.zones || [];
            this.config.program = this.config.program || { };
            this.config.program.cycles = this.config.program.cycles || [];
            this.updateProgram();
        }, err => {
            this.error = format("Garden_ErrorConf", err.message);
        }).finally(() => {
            this.loaded = true;
        });
    }

    private updateProgram(): void {
        this.canResumeAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => c.suspended);
        this.canSuspendAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => !c.suspended);
    }

    public stop() {
        this.checkXhr(this.$http.post<IGardenStartStopResponse>("/svc/gardenStop", "")).then(() => {
            this.message = res["Garden_Stopped"];  
            this.immediateStarted = false;
        }, err => {
            this.error = format("Garden_StopError", err.message);
        });
    }

    public startImmediate() {
        var body = { zones: this.immediateCycle.zones.filter(z => z.enabled).map(z => z.index), time: new Number(this.immediateCycle.time) };
        this.checkXhr(this.$http.post<IGardenStartStopResponse>("/svc/gardenStart", body)).then(() => {
            this.message = res["Garden_StartedImmediate"];  
            this.immediateStarted = true;
            this.loadConfigAndStatus();
        }, err => {
            this.error = format("Garden_ImmediateError", err.message);
        });
    }

    addImmediateCycle(): void {
        this.preCheckPrivilege().then(() => {
            // Mutually exclusive
            this.clearProgram();
            this.immediateCycle = new ImmediateCycle(this.zoneNames, "5");
        }, () => { });
    }

    clearImmediate(): void {
        this.immediateCycle = null;
    }

    resumeAll(): void {
        const now = moment().toISOString(true);
        this.config.program.cycles.forEach(c => {
            c.start = now;
            c.suspended = false;
        });
        this.saveProgram();
    }

    suspendAll(): void {
        this.config.program.cycles.forEach(c => c.suspended = true);
        this.saveProgram();
    }

    public startEdit(): void {
        this.preCheckPrivilege().then(() => {
            this.editProgramMode = true;
            this.clearImmediate();
        }, () => { });
    }

    private saveProgram(): ng.IPromise<void> {
        return this.checkXhr(this.$http.put("/svc/gardenCfg", this.config)).then(() => {
            this.loadConfigAndStatus();
        }, err => {
            this.error = format("Garden_ErrorSetConf", err.message);
        }).finally(() => {
            this.clearProgram();
        })
    }

    private clearProgram(): void {
        this.editProgramMode = false;
    }
}