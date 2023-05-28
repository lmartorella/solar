import { res, format } from "./resources";

import * as Plotly from "plotly.js";

interface IPvData { 
    status: number;
    error?: string;
    mode: number;
    currentW: number;
    fault: number;
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
                    switch (this.pvData.mode) {
                        case undefined:
                        case 0:
                            this.firstLine = res["Solar_Off"];
                            this.firstLineClass = 'gray';
                            break;
                        case 1:
                            this.firstLine = format("Solar_On", { power: this.pvData.currentW });
                            break;
                        case 2:
                            this.firstLine = format("Error", this.decodeFault(this.pvData.fault));
                            this.firstLineClass = 'err';
                            break;
                        default:
                            this.firstLine = format("Solar_UnknownMode", { mode: this.pvData.mode });
                            this.firstLineClass = 'unknown';
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

    private decodeFault(fault: number) {
        switch (fault) { 
            case 0x800:
                return res["Solar_FaultNoGrid"];
            case 0x1000:
                return res["Solar_FaultLowFreq"];
            case 0x2000:
                return res["Solar_FaultHighFreq"];
        }
        return fault && ('0x' + fault.toString(16));
    }
}

