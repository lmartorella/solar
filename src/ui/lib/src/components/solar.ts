import { Component, ElementRef, OnInit, ViewChild } from "@angular/core";
import { res, format } from "../services/resources";
import { HttpClient } from "@angular/common/http";
import { XhrService } from "../services/xhr";

interface IPvData { 
    status: number;
    error?: string;
    
    currentW: number;
    totalKwh: number;
    totalDayWh: number;
    usageA: number;
    gridV: number;

    peakW: number;
    peakWTs: string;
    peakV: number;
    peakVTs: string;

    currentTs: string;
    inverterState: keyof typeof FaultStates | keyof typeof NormalStates | null | "";
}

interface IPvChartPoint {
    ts: string;
    power: number;
    voltage: number;
}

const NormalStates = {
    "OFF": res.Solar_Off,
    "WAIT": res.Solar_Wait,
    "CHK": res.Solar_Check
};

const FaultStates = {
    "NOGRID": res.Solar_FaultNoGrid
};

@Component({
    selector: 'solar-component',
    templateUrl: './solar.html',
    styleUrls: ['./solar.css']
})
export class SolarComponent implements OnInit {
    @ViewChild('chart') public chartElement!: ElementRef<HTMLDivElement>;
    public firstLineClass!: string;
    public firstLine!: string;
    public pvData: Partial<IPvData> = { };
    public readonly today: string;
    public status!: string;
    public loaded!: boolean;
    public readonly res: { [key: string]: string };
    public readonly format: (str: string, args?: any) => string;
    public chartLoading = false;

    constructor(private xhr: XhrService, private readonly http: HttpClient) {
        this.res = res as unknown as { [key: string]: string };
        this.format = format;
        this.today = new Date().toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
    }

    public async ngOnInit() {
        this.status = res["Device_StatusLoading"];
        this.loaded = false;

        try {
            this.pvData = await this.xhr.check(this.http.get<IPvData>(`${this.xhr.baseUrl}/solar/solarStatus`));
            switch (this.pvData.status) {
                case 1: this.status = res["Device_StatusOnline"]; break;
                case 2: this.status = res["Device_StatusOffline"]; break;
                case 3: this.status = res["Device_StatusPartiallyOnline"]; break;
            }
            if (this.pvData.error) {
                this.firstLine = format("Error", this.pvData.error);
                this.firstLineClass = 'err';
            } else {
                const state = this.pvData.inverterState;
                if (state == null || NormalStates[state as keyof typeof NormalStates]) {
                    this.firstLine = NormalStates[state as keyof typeof NormalStates || "OFF"];
                    this.firstLineClass = 'gray';
                } else if (state === undefined || state === "") {
                    this.firstLine = format("Solar_On", { power: this.pvData.currentW });
                } else {
                    this.firstLine = format("Error", this.decodeFault(this.pvData.inverterState as keyof typeof FaultStates));
                    this.firstLineClass = 'err';
                }
            }
        } catch (err) {
            this.firstLine = format("Error", (err as Error).message);
            this.firstLineClass = 'err';
        } finally {
            this.loaded = true;
        }
    }

    private clearChartDom() {
        for (let i = 0; i < this.chartElement.nativeElement.childNodes.length; i++) {
            this.chartElement.nativeElement.removeChild(this.chartElement.nativeElement.childNodes[i]);
        }
    }

    /**
     * @param opts.day can be a string in the yyyy-mm-dd format
     * @param opts.deltaDay is a negative number to apply to today. E.g. -1 means yesterday data, etc...
     */
    private async getDayDataSeries(opts: { deltaDay?: number, day?: string }, seriesName: string): Promise<Partial<Plotly.PlotData> | null> {
        const urlArgs = Object.keys(opts).map(key => `${key}=${encodeURIComponent((opts as any)[key])}`).join("&");
        const data = await this.xhr.check(this.http.get<IPvChartPoint[]>(`${this.xhr.baseUrl}/solar/solarPowToday?${urlArgs}`));
        if (!Array.isArray(data)) {
            throw new Error("Unexpected data format");
        }
        if (!data.length) {
            return null;
        }
        return {
            x: data.map(s => s.ts),
            y: data.map(s => s.power),
            mode: 'lines',
            name: seriesName,
            type: 'scatter',
            hovertext: data.map(s => `${s.power}W, ${s.voltage}V`)
        };
    }

    private async getDaySpanSeries(count: number): Promise<Partial<Plotly.PlotData>[]> {
        // Fetch the last 4 days
        const promises = [] as Promise<Partial<Plotly.PlotData> | null>[];
        for (let deltaDay = -count + 1; deltaDay <= 0; deltaDay++) {
            promises.push(this.getDayDataSeries({ deltaDay }, `T${deltaDay === 0 ? '' : deltaDay}`));
        }
        return (await Promise.all(promises)).filter(r => !!r) as Partial<Plotly.PlotData>[];
    }

    private async getHistoricalSeries(count: number): Promise<Partial<Plotly.PlotData>[]> {
        // Fetch the last 4 days
        const promises = [] as Promise<Partial<Plotly.PlotData> | null>[];
        const today = new Date();

        const pad = (n: number, str: number) => {
            return str.toString().padStart(n, "0");
        };

        for (let deltaYear = -count + 1; deltaYear <= 0; deltaYear++) {
            const day = `${pad(4, today.getFullYear() + deltaYear)}-${pad(2, today.getMonth() + 1)}-${pad(2, today.getDate())}`;
            promises.push(this.getDayDataSeries({ day  }, `Y${deltaYear === 0 ? '' : deltaYear}`));
        }
        return (await Promise.all(promises)).filter(r => !!r) as Partial<Plotly.PlotData>[];
    }

    public async drawDays(opts: { deltaDayCount?: number, historicalCount?: number }) {
        this.clearChartDom();

        this.chartLoading = true;
        try {
            let series: Partial<Plotly.PlotData>[];
            if (opts.historicalCount) {
                series = await this.getHistoricalSeries(opts.historicalCount);
            } else {
                series = await this.getDaySpanSeries(opts.deltaDayCount || 1);
            }

            // Sort 'x' categories in alphabetical order
            const sortCat = (days: Partial<Plotly.PlotData>[]) => {
                var ret = days.reduce((timeStamps, day) => {
                    return (day.x as string[]).reduce((categories, v) => {
                        categories[v] = true;
                        return categories;
                    }, timeStamps);
                }, { } as { [key: string]: boolean });
                return Object.keys(ret).sort();
            };

            const Plotly = await import(
                /* webpackChunkName: "plotly" */
                /* webpackMode: "lazy" */
                "plotly.js"
            );
            Plotly.newPlot(this.chartElement.nativeElement, series, {
                xaxis: {
                    categoryarray: sortCat(series)
                }
            });
        } finally {
            this.chartLoading = false;
        }
    }

    private decodeFault(fault: keyof typeof FaultStates) {
        return FaultStates[fault] || fault;
    }
}

