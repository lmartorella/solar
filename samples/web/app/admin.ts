export class AdminController {
    static $inject = ['$http'];
    constructor(private $http: ng.IHttpService) {
    }

    haltMain() {
        this.$http.get('/svc/haltMain').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    startMain() {
        this.$http.get('/svc/startMain').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restartSolar() {
        this.$http.get('/svc/restartSolar').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restartGarden() {
        this.$http.get('/svc/restartGarden').then(resp => {
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