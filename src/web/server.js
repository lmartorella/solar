const fs = require('fs');
const path = require('path');
const express = require('express');
const passport = require('passport');
const compression = require('compression');
const passportLocal = require('passport-local');
const { logsFile, settings } = require('./settings');
const samples = require('../../samples/web');

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

var app = express();
app.use(express.json());
app.use(compression());
app.use(require('cookie-parser')());
app.use(require('body-parser').urlencoded({ extended: true }));
app.use(require('body-parser').raw({ type: "application/octect-stream" }));
app.use(require('express-session')({ secret: 'keyboard cat', resave: false, saveUninitialized: false }));

app.set("view engine", "ejs");
app.set('views', __dirname);

// Redirect to SPA
app.get('/', (_req, res) => {
    res.redirect('/app/index.html');
});

// Register custom endpoints
samples.register(app, ensureLoggedIn);

app.get('/svc/logs', ensureLoggedIn(), (_req, res) => {
    // Stream log file
    res.setHeader("Content-Type", "text/plain");
    if (fs.existsSync(logsFile)) {
        fs.createReadStream(logsFile).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

app.get('/svc/halt', ensureLoggedIn(), async (_req, res) => {
    try {
        await mainProcess.kill();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Halted");
});

app.get('/svc/start', ensureLoggedIn(), async (_req, res) => {
    try {
        await mainProcess.start();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Started");
});

app.get('/svc/restart', ensureLoggedIn(), async (_req, res) => {
    try {
        await mainProcess.restart();
    } catch (err) {
        res.send("ERR: " + err.message);
        return;
    }
    res.send("Restarted");
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
  console.log('Webserver started at port 80');
})

const runProcesses = async () => {
    const { ManagedProcess } = await import('./procMan.mjs');
    ManagedProcess.enableMail = false;
    const mainProcess = new ManagedProcess('Home.Server.exe');
    const solarProcess = new ManagedProcess('Home.Solar.exe');
    const gardenProcess = new ManagedProcess('Home.Garden.exe');
    mainProcess.start();
    solarProcess.start();
    gardenProcess.start();
};
void runProcesses();
