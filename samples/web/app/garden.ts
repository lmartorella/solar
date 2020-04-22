import { res, format } from "./resources";

declare var moment: any;
moment.locale("it-IT");

interface IGardenStatusResponse {
    error?: string;
    config: {
        zones: string[];
    };
    online: boolean;
    configured: boolean;
    flowData: { 
        totalMc: number;
        flowLMin: number;
    };
    nextCycles: { name: string, scheduledTime: string, suspended: boolean }[];
}

interface IGardenStartStopResponse {
    error: string;
}

class Cycle {
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
    public program: Cycle[] = [];
    public status: string;
    public flow: { 
        totalMc: number;
        flowLMin: number;
    };
    public nextCycles: { name: string, scheduledTime: string, suspended: boolean }[];
    public immediateStarted: boolean;

    static $inject = ['$http', '$scope'];
    constructor(private $http: ng.IHttpService) { }

    public $onInit() {
        this.status = res["Device_StatusLoading"];
        this.loaded = false;

        // Fetch zones
        this.$http.get<IGardenStatusResponse>("/svc/gardenStatus").then(resp => {
            if (resp.status == 200 && resp.data) {
                if (resp.data.error) {
                    this.error = format("Garden_ErrorConf", resp.data.error);
                } else {
                    this.zoneNames = resp.data.config && resp.data.config.zones;
                    this.status =  resp.data.online ? res["Device_StatusOnline"] : (resp.data.config ? res["Device_StatusOffline"] : res["Garden_MissingConf"]);
                    this.flow = resp.data.flowData;
    
                    let now = moment.now();
                    if (resp.data.nextCycles) {
                        this.nextCycles = resp.data.nextCycles;
                        this.nextCycles.forEach(cycle => {
                            cycle.scheduledTime = cycle.scheduledTime && moment.duration(moment(cycle.scheduledTime).diff(now)).humanize(true)
                        })
                    }
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

    start() {
        var body = this.program.map(cycle => ({ zones: cycle.zones.filter(z => z.enabled).map(z => z.index), time: new Number(cycle.time) }));
        this.$http.post<IGardenStartStopResponse>("/svc/gardenStart", JSON.stringify(body)).then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = format("Error", resp.data.error);
                } else {
                    this.message = res["Garden_Started"];  
                    this.immediateStarted = true;
                }
            } else {
                this.error = format("Garden_StartError", '');
            }
        }, err => {
            this.error = format("Garden_StartError", err.statusText);
        });
    }

    addCycle(): void {
        this.program.push(new Cycle(this.zoneNames, "5"));
    }

    removeCycle(index: number): void {
        this.program.splice(index, 1);
    }
}