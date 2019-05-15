const fs = require('fs');
const path = require('path');
const { etcDir } = require('../../src/web/settings');
const procMan = require('../../src/web/procMan');

const gardenCfgFile = path.join(etcDir, 'server/gardenCfg.json');
const gardenCsvFile = path.join(etcDir, 'DB/GARDEN/garden.csv');

function register(app, privileged) {
    app.get('/r/gardenStatus', async (req, res) => {
        let resp = await procMan.sendMessage({ command: "garden.getStatus" });
        res.send(resp);
    });
    
    app.post('/r/gardenStart', privileged(), async (req, res) => {
        let immediate = req.body;
        if (!Array.isArray(immediate) || !immediate.every(v => typeof v === "object") || immediate.every(v => v.time <= 0)) {
            // Do nothing
            res.status(500);
            res.send("Request incompatible");
            console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
            return;
        }
    
        let resp = await procMan.sendMessage({ command: "garden.setImmediate", immediate });
        res.send(resp);
    });
    
    app.post('/r/gardenStop', privileged(), async (req, res) => {
        let resp = await procMan.sendMessage({ command: "garden.stop" });
        res.send(resp);
    });
    
    app.get('/r/gardenCfg', privileged(), async (req, res) => {
        // Stream config file
        const stream = fs.existsSync(gardenCfgFile) && fs.createReadStream(gardenCfgFile);
        if (stream) {
            res.setHeader("Content-Type", "application/json");
            stream.pipe(res);
        } else {
            res.sendStatus(404);
        }
    });
    
    app.get('/r/gardenCsv', privileged(), async (req, res) => {
        // Stream csv file
        const stream = fs.existsSync(gardenCsvFile) && fs.createReadStream(gardenCsvFile);
        if (stream) {
            res.setHeader("Content-Type", "text/csv");
            stream.pipe(res);
        } else {
            res.sendStatus(404);
        }
    });
    
    app.put('/r/gardenCfg', privileged(), async (req, res) => {
        if (req.headers["content-type"] === "application/octect-stream") {
            // Stream back config file
            fs.writeFileSync(gardenCfgFile, req.body);
            res.sendStatus(200);
        } else {
            res.sendStatus(500);
        }
    });    

    // Init default gardenCfg.json file (before starting the native server)
    if (!fs.existsSync(gardenCfgFile)) {
        // Example of configuration (without programs)
        fs.writeFileSync(gardenCfgFile, JSON.stringify({
            "zones": ["Grass (North)", "Grass (South)", "Flowerbeds"]
        }, null, 3));
    } 
}

module.exports = { register };