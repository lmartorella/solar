export const Strings = { 
    "Error": (err: string) => `Errore: ${err}`,

    "Device_StatusLoading": "Caricamento...",
    "Device_StatusError": "ERRORE",
    "Device_StatusPartiallyOnline": "Parzialmente Online",

    "Solar_ChartToday": "Andamento oggi",
    "Solar_Chart4days": "Andamento 4 giorni",
    "Solar_EnergyToday": "Energia oggi:",
    "Solar_EnergyTotal": "Energia totale:",
    "Solar_Updated": (args: any) => `Aggiornato ${args.currentTs}.`,

    "Solar_PeakW1": "Picco di ",
    "Solar_PeakW2": (args: any) => ` alle ${args.ts}`,
    "Solar_PeakV1": "Picco del voltaggio di rete di ",
    "Solar_PeakV2": (args: any) => ` alle ${args.ts}`,

    "Solar_CurrentUsage": "Assorbimento attuale di ",
    "Solar_On": (args: any) => `Potenza: ${args.power}W`,
    "Solar_UnknownMode": (args: any) => `Errore: modalità sconosciuta: ${args.mode}`,
    "Solar_FaultNoGrid": "Mancanza rete",
    "Solar_Off": "Spento",
    "Solar_Wait": "In attesa",
    "Solar_Check": "Controllo linea",
};
