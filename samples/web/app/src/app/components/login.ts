export class LoginComponent {
    public username: string;
    public password: string;

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