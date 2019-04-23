import * as fs from 'fs';
import * as express from 'express';
import * as passport from 'passport';
import * as compression from 'compression';
import * as passportLocal from 'passport-local';
import { setTimeout } from 'timers';
import { getPvData, getPvChart } from './solar';
import { binDir, etcDir, logsFile, gardenCfgFile, gardenCsvFile, serverCfgFile } from './settings';
import ProcessManager from './procMan';

const settings = JSON.parse(fs.readFileSync(serverCfgFile, 'utf8'));

let pm = new ProcessManager(binDir, etcDir, 'Home.Server.App.exe');

passport.use(new passportLocal.Strategy((username: string, password: string, done: (error: any, user?: any) => void) => { 
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
passport.serializeUser<string, string>((user, cb) => {
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
        return res.send(401); // Unauth
      }
      next();
    }
}

var app = express();
app.use((express as any).json());
app.use(compression());
app.use(require('cookie-parser')());
app.use(require('body-parser').urlencoded({ extended: true }));
app.use(require('body-parser').raw({ type: "application/octect-stream" }));
app.use(require('express-session')({ secret: 'keyboard cat', resave: false, saveUninitialized: false }));

app.set("view engine", "ejs");
app.set('views', __dirname);

app.get('/', (req, res) => {
    res.redirect('/app/index.html');
});

app.get('/r/imm', (req, res) => {
    var pvData = getPvData();
    if (pvData.error) {
        res.send({ error: pvData.error });
    } else {
        res.send(pvData);
    }
});

app.get('/r/powToday', (req, res) => {
    setTimeout(() => {
        res.send(getPvChart(req.query && Number(req.query.day)));
    }, 1000);
});

app.get('/r/gardenStatus', async (req, res) => {
    let resp = await pm.sendMessage({ command: "garden.getStatus" });
    res.send(resp);
});

app.post('/r/gardenStart', ensureLoggedIn(), async (req, res) => {
    let immediate = req.body;
    if (!Array.isArray(immediate) || !immediate.every(v => typeof v === "object") || immediate.every(v => v.time <= 0)) {
        // Do nothing
        res.status(500);
        res.send("Request incompatible");
        console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
        return;
    }

    let resp = await pm.sendMessage({ command: "garden.setImmediate", immediate });
    res.send(resp);
});

app.post('/r/gardenStop', ensureLoggedIn(), async (req, res) => {
    let resp = await pm.sendMessage({ command: "garden.stop" });
    res.send(resp);
});

app.get('/r/gardenCfg', ensureLoggedIn(), async (req, res) => {
    // Stream config file
    if (fs.existsSync(gardenCfgFile)) {
        res.setHeader("Content-Type", "application/json");
        fs.createReadStream(gardenCfgFile).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

app.get('/r/gardenCsv', ensureLoggedIn(), async (req, res) => {
    // Stream csv file
    if (fs.existsSync(gardenCsvFile)) {
        res.setHeader("Content-Type", "text/csv");
        fs.createReadStream(gardenCsvFile).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

app.put('/r/gardenCfg', ensureLoggedIn(), async (req, res) => {
    if (req.headers["content-type"] === "application/octect-stream") {
        // Stream back config file
        var content = req.body;
        fs.writeFileSync(gardenCfgFile, content);
        res.sendStatus(200);
    } else {
        res.sendStatus(500);
    }
});

app.get('/r/logs', ensureLoggedIn(), (req, res) => {
    // Stream log file
    res.setHeader("Content-Type", "text/plain");
    if (fs.existsSync(logsFile)) {
        fs.createReadStream(logsFile).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

app.get('/r/halt', ensureLoggedIn(), async (req, res) => {
    try {
        await pm.halt();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Halted");
});

app.get('/r/start', ensureLoggedIn(), async (req, res) => {
    try {
        await pm.start();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Started");
});

app.get('/r/restart', ensureLoggedIn(), async (req, res) => {
    try {
        await pm.restart();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Restarted");
});

app.use('/app', express.static('app'));
app.use('/lib/angular', express.static('node_modules/angular'));
app.use('/lib/moment', express.static('node_modules/moment/min'));

app.use(passport.initialize());
app.use(passport.session());

app.post('/login', passport.authenticate('local'), (req, res) => {
    res.send(200);
});
app.get('/logout', (req, res) => {
    req.logout();
    res.send(401);
});

app.listen(80, () => {
  console.log('Webserver started at port 80');
})

pm.start();
