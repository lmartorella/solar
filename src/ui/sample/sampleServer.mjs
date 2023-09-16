import bodyParser from "body-parser";
import compression from "compression";
import cors from "cors";
import express from "express";
import path from "path";
import process from "process";
import * as solar from "../index.mjs";

const port = 8081;

const app = express();
app.use(express.json());
app.use(compression());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.raw({ type: "application/octect-stream" }));

// Not a production server
const corsOptions = {
    origin: "*",
    methods: "GET,HEAD,PUT,PATCH,POST,DELETE",
    preflightContinue: false,
    optionsSuccessStatus: 204
};

// Register custom endpoints
solar.register({
    get: (url, options) => app.get(url, cors(corsOptions), options)
}, path.join(process.cwd(), "../../../../target/etc/Db/SOLAR"));

app.listen(port, () => {
    console.log(`REST server started at http://localhost:${port}`);
});
