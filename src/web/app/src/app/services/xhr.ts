import { HttpErrorResponse } from "@angular/common/http";
import { Observable, catchError, of } from "rxjs";

export const checkXhr = <T>(observable: Observable<T>): Promise<T> => {
    return new Promise((resolve, reject) => {
        observable.pipe(catchError((err: HttpErrorResponse) => {
            reject(new Error(err.error || err.statusText || err.message));
            return of({ } as T);
        })).subscribe(data => {
            if ((data as T & { error?: string })?.error) {
                reject(new Error((data as T & { error?: string }).error));
            } else if (!data) {
                reject(new Error("Empty response"));
            } else {
                resolve(data);
            }
        });
    });
};
