export const Strings = { 
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
    "Garden_Suspended": " (sospeso)",

    "Solar_ChartToday": "Andamento oggi",
    "Solar_Chart4days": "Andamento 4 giorni",
    "Solar_EnergyToday": "Energia oggi:",
    "Solar_EnergyTotal": "Energia totale:",
    "Solar_Updated": args => `Aggiornato ${args.currentTs}.`,
    "Solar_On": args => `Potenza: ${args.power}W`,
    "Solar_UnknownMode": args => `Errore: modalità sconosciuta: ${args.mode}`,
    "Solar_FaultNoGrid": "Mancanza rete",
    "Solar_FaultLowFreq": "Frequenza rete troppo bassa",
    "Solar_FaultHighFreq": "Frequenza rete troppo alta"
};
