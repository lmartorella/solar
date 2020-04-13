import { SolarController } from "./solar";
import { GardenController } from "./garden";
import * as angular from "angular";
import { MainController } from "./common";
import { AdminController } from "./admin";
import { res, format } from "./resources";

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
    .run(['$rootScope', $rootScope => {
        $rootScope['res'] = res;
        $rootScope['format'] = format;
    }])
    .controller('adminController', AdminController)
    .controller('solarCtrl', SolarController)
    .controller('gardenCtrl', GardenController)
    .controller('mainController', MainController);


