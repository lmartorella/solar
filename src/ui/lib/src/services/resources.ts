import { Strings as itStrings } from './resources.it-IT';

export const Strings = {
    "Error": (err: string) => `Error: ${err}`,

    "Device_StatusLoading": "Loading...",
    "Device_StatusOnline": "Online",
    "Device_StatusPartiallyOnline": "Partially Online",
    "Device_StatusOffline": "OFFLINE",

    "Solar_ChartToday": "Chart today",
    "Solar_Chart4days": "Chart last 4 days",
    "Solar_EnergyToday": "Today's energy:",
    "Solar_EnergyTotal": "Total energy:",
    "Solar_Updated": (args: any) => `Up-to-date at ${args.currentTs}.`,

    "Solar_PeakW1": "Peak of ",
    "Solar_PeakW2": (args: any) => ` at ${args.ts}`,
    "Solar_PeakV1": "Grid peak voltage of ",
    "Solar_PeakV2": (args: any) => ` at ${args.ts}`,

    "Solar_CurrentUsage": "Current usage of ",
    "Solar_On": (args: any) => `Power: ${args.power}W`,
    "Solar_FaultNoGrid": "No grid power",
    "Solar_Off": "OFF",
    "Solar_Wait": "Waiting",
    "Solar_Check": "Grid check"
};

/**
 * TODO: use a proper i18n service
 */
const res = { ...Strings, ...itStrings };
const format = (str: string, args: any) => (typeof (res as any)[str] === "function") ? (res as any)[str](args): (res as any)[str];

export { res, format };
