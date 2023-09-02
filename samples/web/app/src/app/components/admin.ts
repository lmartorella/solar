import { HttpClient } from '@angular/common/http';
import { Component, ViewChild } from '@angular/core';
import { res } from '../services/resources';
import { checkXhr } from '../services/xhr';

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

    public haltMain() {
        checkXhr<unknown>(this.http.get('/svc/halt/server')).then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public startMain() {
        checkXhr<unknown>(this.http.get('/svc/start/server')).then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public restartSolar() {
        checkXhr<unknown>(this.http.get('/svc/restart/solar')).then(data => {
            alert(data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    public restartGarden() {
        checkXhr<unknown>(this.http.get('/svc/restart/garden')).then(data => {
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