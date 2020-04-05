export const Strings = {
    "Menu_Logs": "Logs",
    "Admin_Halt": "Halt Server",
    "Admin_Start": "Start Server",
    "Admin_Restart": "Restart Server",
    "Admin_DownloadGardenCsv": "Download garden.csv",
    "Admin_DownloadGardenConfig": "Download garden configuration",
    "Admin_UploadGardenConfig": "Upload garden configuration",

    "Error": err => `Error: ${err}`,

    "Device_StatusLoading": "Loading...",
    "Device_StatusOnline": "Online",
    "Device_StatusOffline": "OFFLINE",

    "Garden_QuickCycle": "Quick cycle",
    "Garden_Minutes": "Minutes: ",
    "Garden_Add": "Add cycle",
    "Garden_Remove": "Remove",
    "Garden_Start": "Start!",
    "Garden_Started": "Started",
    "Garden_StartError": err => `Error starting: ${err}`,
    "Garden_Stop": "STOP",
    "Garden_Stopped": "Stopped!",
    "Garden_StopError": err => `Error stopping: ${err}`,
    "Garden_NextCycles": "Next programmmed cycles:",
    "Garden_ScheduledProgram": (args) => `${args.name} program scheduled for ${args.scheduledTime}`,
    "Garden_QueuedProgram": (args) => `${args.name} program in queue`,
    "Garden_FlowInfo": "Flow:",
    "Garden_MissingConf": "Missing configuration",
    "Garden_ErrorConf": err => `Cannot fetch configuration: ${err}`,

    "Solar_ChartToday": "Chart today",
    "Solar_Chart4days": "Chart last 4 days",
    "Solar_EnergyToday": "Today's energy:",
    "Solar_EnergyTotal": "Total energy:",
    "Solar_Updated": args => `Update at ${args.currentTsTime} of ${args.currentTsDate}.`,
    "Solar_Off": "OFF",
    "Solar_On": args => `Power: ${args.power}W`,
};

export const it_it_Strings = { 
    "Device_StatusLoading": "Caricamento...",
    "Device_StatusError": "ERRORE",

    "Error": err => `Errore: ${err}`,

    "Garden_QuickCycle": "Programma veloce:",
    "Garden_Minutes": "Minuti: ",
    "Garden_Add": "Aggiungi ciclo",
    "Garden_Remove": "Rimuovi",
    "Garden_Start": "Vai!",
    "Garden_Started": "Avviato",
    "Garden_StartError": err => `Non posso avviare: ${err}`,
    "Garden_Stopped": "Fermato!",
    "Garden_StopError": err => `Non posso fermare: ${err}`,
    "Garden_NextCycles": "Prossime irrigazioni:",
    "Garden_ScheduledProgram": args => `Programma ${args.name} schedulato ${args.scheduledTime}`,
    "Garden_QueuedProgram": args => `Programma ${args.name} in coda`,
    "Garden_FlowInfo": "Flusso:",
    "Garden_MissingConf": "Non configurato",
    "Garden_ErrorConf": err => `Errore accedendo alla configurazione: ${err}`,

    "Solar_ChartToday": "Andamento oggi",
    "Solar_Chart4days": "Andamento 4 giorni",
    "Solar_EnergyToday": "Energia oggi:",
    "Solar_EnergyTotal": "Energia totale:",
    "Solar_Updated": args => `Aggiornato alle ${args.currentTsTime} del ${args.currentTsDate}.`,
    "Solar_On": args => `Potenza: ${args.power}W`,
    "Solar_UnknownMode": args => `Errore: modalità sconosciuta: ${args.mode}`,
};

const res = { ...Strings, ...it_it_Strings };
const format = (str, args) => res[str](args);

export { res, format };
