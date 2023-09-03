
import * as solar from './solar.mjs';
import * as garden from './garden.mjs';
import path from 'path';
import express from 'express';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function register(app, privileged) {
    // Redirect to SPA
    app.get('/', (_req, res) => {
        res.redirect('/app/index.html');
    });
    app.use('/app', express.static(path.join(__dirname, '../../target/webapp')));

    solar.register(app, privileged);
    garden.register(app, privileged);
};
