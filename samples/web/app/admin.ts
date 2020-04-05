export class AdminController {
    static $inject = ['$http'];
    constructor(private $http: ng.IHttpService) {
    }

    halt() {
        this.$http.get('/svc/halt').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    start() {
        this.$http.get('/svc/start').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restart() {
        this.$http.get('/svc/restart').then(resp => {
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