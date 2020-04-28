import { res, format } from "./resources";

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
    suspended: boolean;
}

interface IScheduledCycle extends ICycle {
    scheduledTime: string;
}

interface IGardenStatusResponse {
    error?: string;
    config: IConfig;
    online: boolean;
    configured: boolean;
    flowData: { 
        totalMc: number;
        flowLMin: number;
    };
    nextCycles: IScheduledCycle[];
}

interface IGardenStartStopResponse {
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
    public immediateCycles: ImmediateCycle[] = [];
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

    static $inject = ['$http', '$scope'];
    constructor(private $http: ng.IHttpService) { }

    public $onInit() {
        this.status = res["Device_StatusLoading"];
        this.loaded = false;
        this.loadConfig();
    }

    private loadConfig() {
        // Fetch zones
        this.$http.get<IGardenStatusResponse>("/svc/gardenStatus").then(resp => {
            if (resp.status == 200 && resp.data) {
                if (resp.data.error) {
                    this.error = format("Garden_ErrorConf", resp.data.error);
                } else {
                    this.status =  resp.data.online ? res["Device_StatusOnline"] : (resp.data.config ? res["Device_StatusOffline"] : res["Garden_MissingConf"]);
                    this.flow = resp.data.flowData;
    
                    let now = moment.now();
                    if (resp.data.nextCycles) {
                        this.nextCycles = resp.data.nextCycles;
                        this.nextCycles.forEach(cycle => {
                            cycle.scheduledTime = cycle.scheduledTime && moment.duration(moment(cycle.scheduledTime).diff(now)).humanize(true)
                        })
                    }

                    this.config = resp.data.config || { };
                    this.zoneNames = this.config.zones = this.config.zones || [];
                    this.config.program = this.config.program || { };
                    this.config.program.cycles = this.config.program.cycles || [];
                    this.updateProgram();
                }
            } else {
                this.error = format("Garden_ErrorConf", resp.statusText);
            }
        }, err => {
            this.error = format("Garden_ErrorConf", err.statusText);
        }).finally(() => {
            this.loaded = true;
        });
    }

    private updateProgram(): void {
        this.canResumeAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => c.suspended);
        this.canSuspendAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => !c.suspended);
    }

    stop() {
        this.$http.post<IGardenStartStopResponse>("/svc/gardenStop", "").then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = format("Error", resp.data.error);
                } else {
                    this.message = res["Garden_Stopped"];  
                    this.immediateStarted = false;
                }
            } else {
                this.error = format("Garden_StopError", '');
            }
        }, err => {
            this.error = format("Garden_StopError", err.statusText);
        });
    }

    public startImmediate() {
        var body = this.immediateCycles.map(cycle => ({ zones: cycle.zones.filter(z => z.enabled).map(z => z.index), time: new Number(cycle.time) }));
        this.$http.post<IGardenStartStopResponse>("/svc/gardenStart", JSON.stringify(body)).then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = format("Error", resp.data.error);
                } else {
                    this.message = res["Garden_StartedImmediate"];  
                    this.immediateStarted = true;
                }
            } else {
                this.error = format("Garden_ImmediateError", '');
            }
        }, err => {
            this.error = format("Garden_ImmediateError", err.statusText);
        });
    }

    addImmediateCycle(): void {
        this.immediateCycles.push(new ImmediateCycle(this.zoneNames, "5"));
    }

    removeImmediate(index: number): void {
        this.immediateCycles.splice(index, 1);
    }

    resumeAll(): void {
        this.config.program.cycles.forEach(c => c.suspended = false);
        this.saveProgram();
    }

    suspendAll(): void {
        this.config.program.cycles.forEach(c => c.suspended = true);
        this.saveProgram();
    }

    private saveProgram(): ng.IPromise<void> {
        return this.$http.put("/svc/gardenCfg", this.config).then(() => {
            this.editProgramMode = false;
            this.loadConfig();
        });
    }
}