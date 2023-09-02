import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, ViewChild } from '@angular/core';
import { catchError, of } from 'rxjs';
import { res } from '../services/resources';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.html'
})
export class AdminComponent {
    @ViewChild('file') public fileElement!: HTMLInputElement;
    public readonly res: { [key: string]: string };

    constructor(private readonly http: HttpClient) {
        this.res = res as unknown as { [key: string]: string };
    }

    private get<T>(url: string) {
        return new Promise((resolve, reject) => {
            this.http.get<T>(url).pipe(catchError((err: HttpErrorResponse) => {
                reject(err.error);
                return of(null);
            })).subscribe(data => {
                resolve(data);
            });
        })
    }

    public haltMain() {
        this.get<unknown>('/svc/halt/server').then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public startMain() {
        this.get<unknown>('/svc/start/server').then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public restartSolar() {
        this.get<unknown>('/svc/restart/solar').then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public restartGarden() {
        this.get<unknown>('/svc/restart/garden').then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public sendButton() {
        var req = new XMLHttpRequest();
        req.open("PUT", "/svc/gardenCfg");
        req.setRequestHeader("Content-type", "application/octect-stream");
        req.onload = () => {
            alert('Done');
        };
        req.send(this.fileElement.files![0]);
      }
}