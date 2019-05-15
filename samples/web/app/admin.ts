export class AdminController {
    static $inject = ['$http'];
    constructor(private $http: ng.IHttpService) {
    }

    halt() {
        this.$http.get('/r/halt').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    start() {
        this.$http.get('/r/start').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    restart() {
        this.$http.get('/r/restart').then(resp => {
            alert(resp.data);
        }).catch(err => {
            alert(JSON.stringify(err));
        });
    }

    sendButton() {
        var fileEl = document.getElementById('file') as HTMLInputElement;
        var req = new XMLHttpRequest();
        req.open("PUT", "/r/gardenCfg");
        req.setRequestHeader("Content-type", "application/octect-stream");
        req.onload = () => {
            alert('Done');
        };
        req.send(fileEl.files[0]);
      }
}