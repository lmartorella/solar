export class MainController {
    static $inject = ["$http", "$rootScope"];

    pageUrl: string;
    showLogin: boolean;

    username: string;
    password: string;

    constructor(private $http: ng.IHttpService, $rootScope: { main: MainController }) {
        $rootScope.main = this;
        this.goSolar();
    }

    login(): void {
        this.$http.post("/login", { 
            username: this.username, 
            password: this.password 
        }).then(() => {
            // LOgged in
            this.showLogin = false;
        }).catch(err => {
            alert(err.message || err.data || err.statusText || err);
        });
    }
}