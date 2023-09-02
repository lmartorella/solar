import * as angular from "angular";
import { MainController } from "./common";

angular.module('home', [])
    .service('authInterceptor', ['$q', '$rootScope', function($q: ng.IQService, $rootScope: { main: MainController }) {
        this.responseError = (response) => {
            if (response.status === 401) {
                // External login
                $rootScope.main.showLogin = true;
            }
            return $q.reject(response);
        };
    }])
    .config(['$httpProvider', $httpProvider => {
        $httpProvider.interceptors.push('authInterceptor');
    }])


