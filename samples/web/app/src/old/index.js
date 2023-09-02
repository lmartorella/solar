define("resources.it-IT", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.Strings = void 0;
    exports.Strings = {
        "Device_StatusLoading": "Caricamento...",
        "Device_StatusError": "ERRORE",
        "Device_StatusPartiallyOnline": "Parzialmente Online",
        "Error": err => `Errore: ${err}`,
        "Garden_QuickCycle": "Programma veloce:",
        "Garden_Minutes": "Minuti: ",
        "Garden_AddImmediate": "Aggiungi ciclo manuale",
        "Garden_ClearImmediate": "Cancella",
        "Garden_StartImmediate": "Vai!",
        "Garden_StartedImmediate": "Avviato",
        "Garden_ImmediateError": err => `Non posso avviare: ${err}`,
        "Garden_Stopped": "Fermato!",
        "Garden_StopError": err => `Non posso fermare: ${err}`,
        "Garden_NextCycles": "Prossime irrigazioni:",
        "Garden_ScheduledProgram": args => `Programma ${args.name} schedulato ${args.scheduledTime}`,
        "Garden_RunningProgram": args => `Programma ${args.name} in esecuzione`,
        "Garden_QueuedProgram": args => `Programma ${args.name} in coda`,
        "Garden_FlowInfo": "Flusso:",
        "Garden_MissingConf": "Non configurato",
        "Garden_ErrorConf": err => `Errore accedendo alla configurazione: ${err}`,
        "Garden_ErrorSetConf": err => `Configurazione errata: ${err}`,
        "Garden_Suspended": " (sospeso)",
        "Garden_SuspendAll": "Sospendi per pioggia",
        "Garden_ResumeAll": "Ripristina dopo pioggia",
        "Garden_EditProgram": "Modifica programma",
        "Garden_SuspendedCheckbox": "Sospeso:",
        "Garden_DisabledCheckbox": "Disabilitato:",
        "Garden_StartAt": "Inizio:",
        "Garden_Duration": "Durata (min):",
        "Garden_SaveProgram": "Salva Programma",
        "Garden_ClearProgram": "Cancella",
        "Solar_ChartToday": "Andamento oggi",
        "Solar_Chart4days": "Andamento 4 giorni",
        "Solar_EnergyToday": "Energia oggi:",
        "Solar_EnergyTotal": "Energia totale:",
        "Solar_Updated": args => `Aggiornato ${args.currentTs}.`,
        "Solar_Peak1": "Picco di ",
        "Solar_Peak2": args => ` alle ${args.ts}`,
        "Solar_CurrentUsage": "Assorbimento attuale di ",
        "Solar_On": args => `Potenza: ${args.power}W`,
        "Solar_UnknownMode": args => `Errore: modalità sconosciuta: ${args.mode}`,
        "Solar_FaultNoGrid": "Mancanza rete"
    };
});
define("resources", ["require", "exports", "resources.it-IT"], function (require, exports, resources_it_IT_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.format = exports.res = exports.Strings = void 0;
    exports.Strings = {
        "Admin_Halt": "Halt Net Server",
        "Admin_Start": "Start Net Server",
        "Admin_Restart_Garden": "Restart Garden Server",
        "Admin_Restart_Solar": "Restart Solar Server",
        "Admin_DownloadGardenCsv": "Download garden.csv",
        "Admin_DownloadGardenConfig": "Download garden configuration",
        "Admin_UploadGardenConfig": "Upload garden configuration",
        "Error": err => `Error: ${err}`,
        "Device_StatusLoading": "Loading...",
        "Device_StatusOnline": "Online",
        "Device_StatusPartiallyOnline": "Partially Online",
        "Device_StatusOffline": "OFFLINE",
        "Garden_QuickCycle": "Quick cycle",
        "Garden_Minutes": "Minutes: ",
        "Garden_AddImmediate": "Add manual cycle",
        "Garden_ClearImmediate": "Clear",
        "Garden_StartImmediate": "Go!",
        "Garden_StartedImmediate": "Started",
        "Garden_ImmediateError": err => `Error starting: ${err}`,
        "Garden_Stop": "STOP",
        "Garden_Stopped": "Stopped!",
        "Garden_StopError": err => `Error stopping: ${err}`,
        "Garden_NextCycles": "Next programmmed cycles:",
        "Garden_ScheduledProgram": (args) => `${args.name} program scheduled for ${args.scheduledTime}`,
        "Garden_RunningProgram": (args) => `${args.name} program running`,
        "Garden_QueuedProgram": (args) => `${args.name} program in queue`,
        "Garden_FlowInfo": "Flow:",
        "Garden_MissingConf": "Missing configuration",
        "Garden_ErrorConf": err => `Cannot fetch configuration: ${err}`,
        "Garden_ErrorSetConf": err => `Invalid configuration data: ${err}`,
        "Garden_Suspended": " (suspended)",
        "Garden_SuspendAll": "Suspend for Rain",
        "Garden_ResumeAll": "Resume from Rain",
        "Garden_EditProgram": "Edit program",
        "Garden_SuspendedCheckbox": "Suspended:",
        "Garden_DisabledCheckbox": "Disabled:",
        "Garden_StartAt": "Start at:",
        "Garden_Duration": "Duration (min):",
        "Garden_SaveProgram": "Save Program",
        "Garden_ClearProgram": "Clear",
        "Solar_ChartToday": "Chart today",
        "Solar_Chart4days": "Chart last 4 days",
        "Solar_EnergyToday": "Today's energy:",
        "Solar_EnergyTotal": "Total energy:",
        "Solar_Updated": args => `Up-to-date at ${args.currentTs}.`,
        "Solar_Peak1": "Peak of ",
        "Solar_Peak2": args => ` at ${args.ts}`,
        "Solar_CurrentUsage": "Current usage of ",
        "Solar_Off": "OFF",
        "Solar_On": args => `Power: ${args.power}W`,
        "Solar_FaultNoGrid": "No grid power"
    };
    const res = Object.assign(Object.assign({}, exports.Strings), resources_it_IT_1.Strings);
    exports.res = res;
    const format = (str, args) => (typeof res[str] === "function") ? res[str](args) : res[str];
    exports.format = format;
});
define("solar", ["require", "exports", "resources", "plotly.js"], function (require, exports, resources_1, Plotly) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SolarController = void 0;
    class SolarController {
        constructor($http, $q) {
            this.$http = $http;
            this.$q = $q;
        }
        $onInit() {
            this.status = resources_1.res["Device_StatusLoading"];
            this.loaded = false;
            this.$http.get('/svc/solarStatus').then(resp => {
                if (resp.status == 200 && resp.data) {
                    this.pvData = resp.data;
                    switch (resp.data.status) {
                        case 1:
                            this.status = resources_1.res["Device_StatusOnline"];
                            break;
                        case 2:
                            this.status = resources_1.res["Device_StatusOffline"];
                            break;
                        case 3:
                            this.status = resources_1.res["Device_StatusPartiallyOnline"];
                            break;
                    }
                    if (this.pvData.error) {
                        this.firstLine = (0, resources_1.format)("Error", this.pvData.error);
                        this.firstLineClass = 'err';
                    }
                    else {
                        switch (this.pvData.inverterState) {
                            case "OFF":
                                this.firstLine = resources_1.res["Solar_Off"];
                                this.firstLineClass = 'gray';
                                break;
                            case undefined:
                            case "":
                                this.firstLine = (0, resources_1.format)("Solar_On", { power: this.pvData.currentW });
                                break;
                            default:
                                this.firstLine = (0, resources_1.format)("Error", this.decodeFault(this.pvData.inverterState));
                                this.firstLineClass = 'err';
                                break;
                        }
                    }
                }
                else {
                    this.firstLine = (0, resources_1.format)("Error", resp.data || resp.statusText);
                    this.firstLineClass = 'err';
                }
            }, err => {
                this.firstLine = (0, resources_1.format)("Error", err.data || err.statusText);
                this.firstLineClass = 'err';
            }).finally(() => {
                this.loaded = true;
            });
        }
        drawDays(count) {
            let el = document.getElementById('chart');
            for (let i = 0; i < el.childNodes.length; i++) {
                el.removeChild(el.childNodes[i]);
            }
            // Fetch the last 4 days
            var promises = [];
            for (let day = -count + 1; day <= 0; day++) {
                promises.push(this.$http.get('/svc/solarPowToday?day=' + day).then(resp => {
                    if (resp.status == 200 && Array.isArray(resp.data) && resp.data.length) {
                        return {
                            x: resp.data.map(s => s.ts),
                            y: resp.data.map(s => s.value),
                            mode: 'lines',
                            name: 'T' + (day === 0 ? '' : day.toString()),
                            type: 'scatter'
                        };
                    }
                }));
            }
            function sortCat(series) {
                var ret = series.reduce((times, serie) => {
                    return serie.x.reduce((times, v) => {
                        times[v] = true;
                        return times;
                    }, times);
                }, {});
                return Object.getOwnPropertyNames(ret).sort();
            }
            this.$q.all(promises).then(res => {
                var series = res.filter(r => !!r);
                Plotly.newPlot(el, series, {
                    xaxis: {
                        categoryarray: sortCat(series)
                    }
                });
            });
        }
        decodeFault(fault) {
            switch (fault) {
                case "NOGRID":
                    return resources_1.res["Solar_FaultNoGrid"];
                default:
                    return fault;
            }
        }
    }
    SolarController.$inject = ['$http', '$q', '$scope'];
    exports.SolarController = SolarController;
});
define("garden", ["require", "exports", "resources"], function (require, exports, resources_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GardenController = void 0;
    moment.locale("it-IT");
    class ImmediateCycle {
        constructor(zoneNames, time) {
            this.time = time;
            this.zones = zoneNames.map((name, index) => ({ name, index }));
        }
    }
    class GardenController {
        constructor($http, $q) {
            this.$http = $http;
            this.$q = $q;
            this.zoneNames = [];
        }
        $onInit() {
            this.status1 = resources_2.res["Device_StatusLoading"];
            this.loaded = false;
            this.loadConfigAndStatus();
        }
        checkXhr(xhr) {
            return xhr.then(resp => {
                if (resp.status == 200 && resp.data) {
                    if (resp.data.error) {
                        throw new Error(resp.data.error);
                    }
                    else {
                        return resp.data;
                    }
                }
                else {
                    throw new Error(resp.data.toString() || resp.statusText);
                }
            }, err => {
                throw new Error(err.data || err.statusText || err.message);
            });
        }
        preCheckPrivilege() {
            if (this._hasPrivilege) {
                return this.$q.when();
            }
            else {
                return this.checkXhr(this.$http.get("/checkLogin")).then(() => {
                    this._hasPrivilege = true;
                });
            }
        }
        loadConfigAndStatus() {
            // Fetch zones
            this.checkXhr(this.$http.get("/svc/gardenStatus")).then(resp => {
                switch (resp.status) {
                    case 1:
                        this.status1 = resources_2.res["Device_StatusOnline"];
                        break;
                    case 2:
                        this.status1 = resources_2.res["Device_StatusOffline"];
                        break;
                    case 3:
                        this.status1 = resources_2.res["Device_StatusPartiallyOnline"];
                        break;
                }
                this.status2 = !resp.config && resources_2.res["Garden_MissingConf"];
                this.flow = resp.flowData;
                this.isRunning = resp.isRunning;
                let now = moment.now();
                if (resp.nextCycles) {
                    this.nextCycles = resp.nextCycles;
                    this.nextCycles.forEach(cycle => {
                        cycle.scheduledTime = cycle.scheduledTime && moment.duration(moment(cycle.scheduledTime).diff(now)).humanize(true);
                    });
                }
                this.config = resp.config || {};
                this.zoneNames = this.config.zones = this.config.zones || [];
                this.config.program = this.config.program || {};
                this.config.program.cycles = this.config.program.cycles || [];
                this.updateProgram();
            }, err => {
                this.error = (0, resources_2.format)("Garden_ErrorConf", err.message);
            }).finally(() => {
                this.loaded = true;
            });
        }
        updateProgram() {
            this.canResumeAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => c.suspended);
            this.canSuspendAll = this.config.program.cycles.length > 0 && this.config.program.cycles.some(c => !c.suspended);
        }
        stop() {
            this.checkXhr(this.$http.post("/svc/gardenStop", "")).then(() => {
                this.message = resources_2.res["Garden_Stopped"];
                this.immediateStarted = false;
            }, err => {
                this.error = (0, resources_2.format)("Garden_StopError", err.message);
            });
        }
        startImmediate() {
            var body = { zones: this.immediateCycle.zones.filter(z => z.enabled).map(z => z.index), time: new Number(this.immediateCycle.time) };
            this.checkXhr(this.$http.post("/svc/gardenStart", body)).then(() => {
                this.message = resources_2.res["Garden_StartedImmediate"];
                this.immediateStarted = true;
                this.loadConfigAndStatus();
            }, err => {
                this.error = (0, resources_2.format)("Garden_ImmediateError", err.message);
            });
        }
        addImmediateCycle() {
            this.preCheckPrivilege().then(() => {
                // Mutually exclusive
                this.clearProgram();
                this.immediateCycle = new ImmediateCycle(this.zoneNames, "5");
            }, () => { });
        }
        clearImmediate() {
            this.immediateCycle = null;
        }
        resumeAll() {
            const now = moment().toISOString(true);
            this.config.program.cycles.forEach(c => {
                c.start = now;
                c.suspended = false;
            });
            this.saveProgram();
        }
        suspendAll() {
            this.config.program.cycles.forEach(c => c.suspended = true);
            this.saveProgram();
        }
        startEdit() {
            this.preCheckPrivilege().then(() => {
                this.editProgramMode = true;
                this.clearImmediate();
            }, () => { });
        }
        saveProgram() {
            return this.checkXhr(this.$http.put("/svc/gardenCfg", this.config)).then(() => {
                this.loadConfigAndStatus();
            }, err => {
                this.error = (0, resources_2.format)("Garden_ErrorSetConf", err.message);
            }).finally(() => {
                this.clearProgram();
            });
        }
        clearProgram() {
            this.editProgramMode = false;
        }
    }
    GardenController.$inject = ['$http', '$q'];
    exports.GardenController = GardenController;
});
define("common", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.MainController = void 0;
    class MainController {
        constructor($http, $rootScope) {
            this.$http = $http;
            $rootScope.main = this;
            this.goSolar();
        }
        login() {
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
        goSolar() {
            this.pageUrl = "solar.tpl.html";
        }
        goGarden() {
            this.pageUrl = "garden.tpl.html";
        }
        goAdmin() {
            this.pageUrl = "admin.tpl.html";
        }
    }
    MainController.$inject = ["$http", "$rootScope"];
    exports.MainController = MainController;
});
define("admin", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.AdminController = void 0;
    class AdminController {
        constructor($http) {
            this.$http = $http;
        }
        haltMain() {
            this.$http.get('/svc/halt/server').then(resp => {
                alert(resp.data);
            }).catch(err => {
                alert(JSON.stringify(err));
            });
        }
        startMain() {
            this.$http.get('/svc/start/server').then(resp => {
                alert(resp.data);
            }).catch(err => {
                alert(JSON.stringify(err));
            });
        }
        restartSolar() {
            this.$http.get('/svc/restart/solar').then(resp => {
                alert(resp.data);
            }).catch(err => {
                alert(JSON.stringify(err));
            });
        }
        restartGarden() {
            this.$http.get('/svc/restart/garden').then(resp => {
                alert(resp.data);
            }).catch(err => {
                alert(JSON.stringify(err));
            });
        }
        sendButton() {
            var fileEl = document.getElementById('file');
            var req = new XMLHttpRequest();
            req.open("PUT", "/svc/gardenCfg");
            req.setRequestHeader("Content-type", "application/octect-stream");
            req.onload = () => {
                alert('Done');
            };
            req.send(fileEl.files[0]);
        }
    }
    AdminController.$inject = ['$http'];
    exports.AdminController = AdminController;
});
define("index", ["require", "exports", "solar", "garden", "angular", "common", "admin", "resources"], function (require, exports, solar_1, garden_1, angular, common_1, admin_1, resources_3) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    angular.module('home', [])
        .service('authInterceptor', ['$q', '$rootScope', function ($q, $rootScope) {
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
            $rootScope['res'] = resources_3.res;
            $rootScope['format'] = resources_3.format;
        }])
        .controller('adminController', admin_1.AdminController)
        .controller('solarCtrl', solar_1.SolarController)
        .controller('gardenCtrl', garden_1.GardenController)
        .controller('mainController', common_1.MainController);
});
//# sourceMappingURL=index.js.map