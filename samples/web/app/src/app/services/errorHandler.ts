import { ErrorHandler, Injectable } from "@angular/core";

@Injectable()
export class AlertErrorHandler implements ErrorHandler {
    public handleError(error: any) {
        alert((error as Error).message);
    }
}
