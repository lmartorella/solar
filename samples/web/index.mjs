
import * as solar from './solar.mjs';
import * as garden from './garden.mjs';

export function register(app, privileged) {
    solar.register(app, privileged);
    garden.register(app, privileged);
};
