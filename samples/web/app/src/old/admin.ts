export class AdminController {
    static $inject = ['$http'];
    constructor(private $http: ng.IHttpService) {
    }

    haltMain() {
        this.$http.get('/svc/halt/server').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    startMain() {
        this.$http.get('/svc/start/server').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restartSolar() {
        this.$http.get('/svc/restart/solar').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restartGarden() {
        this.$http.get('/svc/restart/garden').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    sendButton() {
        var fileEl = document.getElementById('file') as HTMLInputElement;
        var req = new XMLHttpRequest();
        req.open("PUT", "/svc/gardenCfg");
        req.setRequestHeader("Content-type", "application/octect-stream");
        req.onload = () => {
            alert('Done');
        };
        req.send(fileEl.files[0]);
      }
}