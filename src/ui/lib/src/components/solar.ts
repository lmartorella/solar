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
    peakTsTime: string;

    currentTs: string;
    inverterState: string;
}

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
    public status!: string;
    public loaded!: boolean;
    public readonly res: { [key: string]: string };
    public readonly format: (str: string, args?: any) => string;
    public chartLoading = false;

    constructor(private xhr: XhrService, private readonly http: HttpClient) {
        this.res = res as unknown as { [key: string]: string };
        this.format = format;
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
                switch (this.pvData.inverterState) {
                    case null:
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

    private async getDayDataSeries(day: number): Promise<Partial<Plotly.PlotData> | null> {
        const data = await this.xhr.check(this.http.get<IPvData>(`${this.xhr.baseUrl}/solar/solarPowToday?day=${day}`));
        if (!Array.isArray(data)) {
            throw new Error("Unexpected data format");
        }
        if (!data.length) {
            return null;
        }
        return {
            x: data.map(s => s.ts),
            y: data.map(s => s.value),
            mode: 'lines',
            name: 'T' + (day === 0 ? '' : day.toString()),
            type: 'scatter'
        };
    }

    public async drawDays(count: number) {
        this.clearChartDom();

        this.chartLoading = true;
        try {
            // Fetch the last 4 days
            const promises = [] as Promise<Partial<Plotly.PlotData> | null>[];
            for (let day = -count + 1; day <= 0; day++) {
                promises.push(this.getDayDataSeries(day));
            }
            const series = (await Promise.all(promises)).filter(r => !!r) as Partial<Plotly.PlotData>[];

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

    private decodeFault(fault: string) {
        switch (fault) { 
            case "NOGRID":
                return res["Solar_FaultNoGrid"];
            default:
                return fault;
        }
    }
}

