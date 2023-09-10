import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { Observable, catchError, throwError } from "rxjs";

@Injectable()
export class LoginInterceptor implements HttpInterceptor {
    constructor(private readonly router: Router) {

    }

    public intercept(httpRequest: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(httpRequest).pipe(catchError((err: HttpErrorResponse) => {
            if (err.status === 401) {
                // External login
                if (this.router.url !== "/login") {
                    this.router.navigateByUrl("/login");
                }
            }
            return throwError(err);
        }));
    }
}