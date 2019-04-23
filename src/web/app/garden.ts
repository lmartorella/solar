declare var moment: any;
moment.locale("it-IT");

interface IGardenStatusResponse {
    config: {
        zones: string[];
    };
    online: boolean;
    configured: boolean;
    flowData: { 
        totalMc: number;
        flowLMin: number;
    };
    nextCycles: { name: string, scheduledTime: string }[];
}

interface IGardenStartStopResponse {
    error: string;
}

class Cycle {
    constructor(zoneNames: string[]) {
        this.zones = zoneNames.map((name, index) => ({ name, index }))
    }

    time: number = 0;

    zones: { 
        name: string;
        enabled?: boolean;
        index: number;
    }[];
}

export class GardenController {
    
    public message: string;
    public error: string;
    private zoneNames: string[] = [];
    private program: Cycle[] = [];
    public status: string;
    public disableButton = true;
    public flow: { 
        totalMc: number;
        flowLMin: number;
    };
    public nextCycles: { name: string, scheduledTime: string }[];

    static $inject = ['$http', '$scope'];
    constructor(private $http: ng.IHttpService) {
        // Fetch zones
        this.$http.get<IGardenStatusResponse>("/r/gardenStatus").then(resp => {
            if (resp.status == 200 && resp.data) {
                this.zoneNames = resp.data.config && resp.data.config.zones;
                this.status =  resp.data.online ? 'Online' : (resp.data.config ? 'OFFLINE' : 'NOT CONFIGURED');
                this.flow = resp.data.flowData;
                this.disableButton = false;

                let now = moment.now();
                if (resp.data.nextCycles) {
                    this.nextCycles = resp.data.nextCycles.map(({ name, scheduledTime }) => {
                        return { name, scheduledTime: scheduledTime && moment.duration(moment(scheduledTime).diff(now)).humanize(true) };
                    });
                }
            } else {
                this.error = "Cannot fetch cfg";
            }
        }, err => {
            this.error = "Cannot fetch cfg: " + err.statusText;
        });
    }

    public isDisableButton() {
        return this.disableButton || this.program.length === 0;
    }

    stop() {
        this.disableButton = true;
        this.$http.post<IGardenStartStopResponse>("/r/gardenStop", "").then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = "ERROR: " + resp.data.error;
                } else {
                    this.message = "Fermato!";
                }
            } else {
                this.error = "Cannot stop garden!";
            }
        }, err => {
            this.error = "Cannot stop garden: " + err.statusText;
        });
    }

    start() {
        this.disableButton = true;
        var body = this.program.map(cycle => ({ zones: cycle.zones.filter(z => z.enabled).map(z => z.index), time: new Number(cycle.time) }));
        this.$http.post<IGardenStartStopResponse>("/r/gardenStart", JSON.stringify(body)).then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = "ERROR: " + resp.data.error;
                } else {
                    this.message = "Avviato!";
                }
            } else {
                this.error = "Cannot start garden!";
            }
        }, err => {
            this.error = "Cannot start garden: " + err.statusText;
        });
    }

    addCycle(): void {
        this.program.push(new Cycle(this.zoneNames));
    }

    removeCycle(index: number): void {
        this.program.splice(index, 1);
    }
}