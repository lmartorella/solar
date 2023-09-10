import { Component } from "@angular/core";
import { checkXhr } from "../services/xhr";
import { HttpClient } from "@angular/common/http";
import { Location } from "@angular/common";

@Component({
    selector: 'app-login',
    templateUrl: './login.html'
})
export class LoginComponent {
    public username!: string;
    public password!: string;

    constructor(private readonly http: HttpClient, private location: Location) {
    }

    public async login() {
        try {
            await checkXhr(this.http.post("/login", {
                username: this.username, 
                password: this.password 
            }, {
                responseType: "text"
            }));
            // Logged in
            this.location.back();
        } catch (err) {
            alert((err as Error).message);
        }
    }
}
