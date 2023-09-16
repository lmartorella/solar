import { HttpErrorResponse } from "@angular/common/http";
import { Inject, Injectable } from "@angular/core";
import { Observable, catchError, of } from "rxjs";
import { ISolarModuleConfig } from "../solar.module";

@Injectable()
export class XhrService {
    constructor(@Inject('config') config: ISolarModuleConfig) {
        this.baseUrl = config?.baseUrl;
    }

    public readonly baseUrl?: string;

    public check<T>(observable: Observable<T>): Promise<T> {
        return new Promise((resolve, reject) => {
            observable.pipe(catchError((err: HttpErrorResponse) => {
                reject(new Error(err.statusText || err.error || err.message));
                return of({ } as T);
            })).subscribe(data => {
                if ((data as T & { error?: string })?.error) {
                    reject(new Error((data as T & { error?: string }).error));
                } else if (!data) {
                    reject(new Error("Server down"));
                } else {
                    resolve(data);
                }
            });
        });
    };
}
