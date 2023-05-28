import fs from 'fs';
import path from 'path';
import express from 'express';
import passport from 'passport';
import compression from 'compression';
import passportLocal from 'passport-local';
import cookieParser from 'cookie-parser';
import bodyParser from 'body-parser';
import expressSession from 'express-session';
import { webLogsFile, settings, __dirname, logger } from './settings.mjs';
import * as samples from '../../samples/web/index.mjs';

passport.use(new passportLocal.Strategy((username, password, done) => { 
    if (username !== settings.username || password !== settings.password) {
        return done("Bad credentials");
    }
    return done(null, username);
}));

function validateUser(user) {
    return (user === settings.username);
}

// Configure Passport authenticated session persistence.
// In order to restore authentication state across HTTP requests, Passport needs
// to serialize users into and deserialize users out of the session.  The
// typical implementation of this is as simple as supplying the user ID when
// serializing, and querying the user record by ID from the database when
// deserializing.
passport.serializeUser((user, cb) => {
    cb(null, user);
});

passport.deserializeUser(function(id, cb) {
    if (!validateUser(id)) {
        return cb('Not logged in');
    }
    cb(null, id);
});

function ensureLoggedIn() {
    return function(req, res, next) {
      if (!validateUser(req.session && req.session.passport && req.session.passport.user)) {
        if (req.session) {
          req.session.returnTo = req.originalUrl || req.url;
        }
        return res.sendStatus(401); // Unauth
      }
      next();
    }
}

const app = express();
app.use(express.json());
app.use(compression());
app.use(cookieParser());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.raw({ type: "application/octect-stream" }));
app.use(expressSession({ secret: 'keyboard cat', resave: false, saveUninitialized: false }));

app.set("view engine", "ejs");
app.set('views', __dirname);

// Redirect to SPA
app.get('/', (_req, res) => {
    res.redirect('/app/index.html');
});

// Register custom endpoints
samples.register(app, ensureLoggedIn);

let serverProcess;
let solarProcess;
let gardenProcess;

app.get('/svc/logs/:id', ensureLoggedIn(), (req, res) => {
    let file;
    switch (req.params.id) {
        case "web": file = webLogsFile; break; 
        case "server": file = serverProcess.logFile; break; 
        case "solar": file = solarProcess.logFile; break; 
        case "garden": file = gardenProcess.logFile; break; 
    }
    if (file && fs.existsSync(file)) {
        // Stream log file
        res.setHeader("Content-Type", "text/plain");
        fs.createReadStream(file).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

const getProcess = id => {
    switch (id) {
        case "server": return serverProcess; 
        case "solar": return solarProcess; 
        case "garden": return gardenProcess; 
    }
}

app.get('/svc/halt/:id', ensureLoggedIn(), async (req, res) => {
    const id = req.params.id;
    const process = getProcess(id);
    if (process) {
        try {
            await process.kill();
        } catch (err) {
            res.send("ERR: " + err.message);
            return;
        }
        res.send(`${id} halted`);
    } else {
        res.sendStatus(404);
    }
});

app.get('/svc/start/:id', ensureLoggedIn(), async (req, res) => {
    const id = req.params.id;
    const process = getProcess(id);
    if (process) {
        try {
            await process.start();
        } catch (err) {
            res.send("ERR: " + err.message);
            return;
        }
        res.send(`${id} started`);
    } else {
        res.sendStatus(404);
    }
});

app.get('/svc/restart/:id', ensureLoggedIn(), async (req, res) => {
    const id = req.params.id;
    const process = getProcess(id);
    if (process) {
        try {
            await process.restart();
        } catch (err) {
            res.send("ERR: " + err.message);
            return;
        }
        res.send(`${id} restarted`);
    } else {
        res.sendStatus(404);
    }
});

app.get('/svc/restart/:id', ensureLoggedIn(), async (req, res) => {
    const id = req.params.id;
    const process = getProcess(id);
    if (process) {
        try {
            await process.restart();
        } catch (err) {
            res.send("ERR: " + err.message);
            return;
        }
        res.send(`${id} restarted`);
    } else {
        res.sendStatus(404);
    }
});


app.use('/app', express.static(path.join(__dirname, '../../samples/web/app')));
app.use('/lib/angular', express.static(path.join(__dirname, '../../node_modules/angular')));
app.use('/lib/moment', express.static(path.join(__dirname, '../../node_modules/moment/min')));
app.use('/lib/plotly.js', express.static(path.join(__dirname, '../../node_modules/plotly.js/dist')));
app.use('/lib/requirejs', express.static(path.join(__dirname, '../../node_modules/requirejs')));

app.use(passport.initialize());
app.use(passport.session());

app.post('/login', passport.authenticate('local'), (req, res) => {
    res.sendStatus(200);
});
app.get('/logout', (req, res) => {
    req.logout();
    res.sendStatus(401);
});
app.get('/checkLogin', ensureLoggedIn(), (_req, res) => {
    res.sendStatus(200);
});

app.listen(80, () => {
  logger('Webserver started at port 80');
})

const runProcesses = async () => {
    const { ManagedProcess } = await import('./procMan.mjs');
    ManagedProcess.enableMail = false;
    serverProcess = new ManagedProcess('Home.Server', 'server');
    solarProcess = new ManagedProcess('Home.Solar', 'solar');
    gardenProcess = new ManagedProcess('Home.Garden', 'garden');
    serverProcess.start();
    solarProcess.start();
    gardenProcess.start();
};
void runProcesses();
