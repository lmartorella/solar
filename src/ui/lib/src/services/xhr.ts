import { HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, catchError, of } from "rxjs";

export interface ISolarModuleConfig {
    baseUrl?: string;
  }
  
export const config: ISolarModuleConfig = { };

@Injectable({ 
    providedIn: 'root'
})
export class XhrService {
    public get baseUrl(): string {
        return config.baseUrl || "";
    }

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
