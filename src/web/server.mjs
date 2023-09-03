import fs from 'fs';
import process from 'process';
import express from 'express';
import passport from 'passport';
import compression from 'compression';
import passportLocal from 'passport-local';
import cookieParser from 'cookie-parser';
import bodyParser from 'body-parser';
import expressSession from 'express-session';
import { webLogsFile, settings, __dirname, logger } from './settings.mjs';
import * as samples from '../../samples/web/index.mjs';

const hasProcessManager = process.argv.indexOf("--no-proc-man") < 0;

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

const ensureLoggedIn = () => {
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
const secret = process.env["EXPRESS_SECRET"];
if (!secret) {
    throw new Error("You need to set the `EXPRESS_SECRET` environment variable");
}
app.use(expressSession({ secret, resave: false, saveUninitialized: false }));

// Register custom endpoints
samples.register(app, ensureLoggedIn);

app.use(passport.initialize());
app.use(passport.session());

app.post('/login', passport.authenticate('local'), (req, res) => {
    res.status(200).send("OK");
});
app.get('/logout', (req, res) => {
    req.logout();
    res.sendStatus(401);
});

let serverProcess;
let solarProcess;
let gardenProcess;

if (hasProcessManager) {
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
            process.kill(res);
        } else {
            res.sendStatus(404);
        }
    });

    app.get('/svc/start/:id', ensureLoggedIn(), async (req, res) => {
        const id = req.params.id;
        const process = getProcess(id);
        if (process) {
            process.start(res);
        } else {
            res.sendStatus(404);
        }
    });

    app.get('/svc/restart/:id', ensureLoggedIn(), async (req, res) => {
        const id = req.params.id;
        const process = getProcess(id);
        if (process) {
            process.restart(res);
        } else {
            res.sendStatus(404);
        }
    });
}

app.listen(80, () => {
  logger('Webserver started at port 80');
})

if (hasProcessManager) {
    const runProcesses = async () => {
        const { ManagedProcess } = await import('./procMan.mjs');
        ManagedProcess.enableMail = false;
        serverProcess = new ManagedProcess('Home.Server', 'server');
        solarProcess = new ManagedProcess('Home.Solar', 'solar');
        gardenProcess = new ManagedProcess('Home.Garden', 'garden');
        serverProcess._start();
        solarProcess._start();
        gardenProcess._start();
    };
    void runProcesses();
}
