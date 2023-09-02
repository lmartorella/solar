import { res, format } from "./resources";

import * as Plotly from "plotly.js";

interface IPvData { 
    status: number;
    error?: string;
    currentW: number;
    inverterState: string;
}

export class SolarController {

    public firstLineClass: string;
    public firstLine: string;
    private pvData: IPvData;
    public status: string;
    public loaded: boolean;

    static $inject = ['$http', '$q', '$scope'];
    constructor(private $http: ng.IHttpService, private $q: ng.IQService) { }

    public $onInit() {
        this.status = res["Device_StatusLoading"];
        this.loaded = false;

        this.$http.get<IPvData>('/svc/solarStatus').then(resp => {
            if (resp.status == 200 && resp.data) {
                this.pvData = resp.data;
                switch (resp.data.status) {
                    case 1: this.status = res["Device_StatusOnline"]; break;
                    case 2: this.status = res["Device_StatusOffline"]; break;
                    case 3: this.status = res["Device_StatusPartiallyOnline"]; break;
                }
                if (this.pvData.error) {
                    this.firstLine = format("Error", this.pvData.error);
                    this.firstLineClass = 'err';
                } else {
                    switch (this.pvData.inverterState) {
                        case "OFF":
                            this.firstLine = res["Solar_Off"];
                            this.firstLineClass = 'gray';
                            break;
                        case undefined:
                        case "":
                            this.firstLine = format("Solar_On", { power: this.pvData.currentW });
                            break;
                        default:
                            this.firstLine = format("Error", this.decodeFault(this.pvData.inverterState));
                            this.firstLineClass = 'err';
                            break;
                    }
                }
            } else {
                this.firstLine = format("Error", resp.data || resp.statusText);
                this.firstLineClass = 'err';
            }
        }, err => {
            this.firstLine = format("Error", err.data || err.statusText);
            this.firstLineClass = 'err';
        }).finally(() => {
            this.loaded = true;
        });
    }

    drawDays(count) {
        let el = document.getElementById('chart');
        for (let i = 0; i < el.childNodes.length; i++) {
            el.removeChild(el.childNodes[i]);
        }

        // Fetch the last 4 days
        var promises = [];
        for (let day = -count + 1; day <= 0; day++) {
            promises.push(this.$http.get('/svc/solarPowToday?day=' + day).then(resp => {
                if (resp.status == 200 && Array.isArray(resp.data) && resp.data.length) {
                    return {
                        x: resp.data.map(s => s.ts),
                        y: resp.data.map(s => s.value),
                        mode: 'lines',
                        name: 'T' + (day === 0 ? '' : day.toString()),
                        type: 'scatter'
                    };
                }
            }));
        }

        function sortCat(series) {
            var ret = series.reduce((times, serie) => {
                return serie.x.reduce((times, v) => {
                    times[v] = true;
                    return times;             
                }, times);
            }, { });
            return Object.getOwnPropertyNames(ret).sort();
        }

        this.$q.all(promises).then(res => {
            var series = res.filter(r => !!r);
            Plotly.newPlot(el, series, {
                xaxis: {
                    categoryarray: sortCat(series)
                }
            });
        });
    }

    private decodeFault(fault: string) {
        switch (fault) { 
            case "NOGRID":
                return res["Solar_FaultNoGrid"];
            default:
                return fault;
        }
    }
}

