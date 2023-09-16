import express from "express";
import compression from "compression";
import bodyParser from "body-parser";
import * as solar from "../index.mjs";
import cors from "cors";

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
}, "../dist/csv");

app.listen(port, () => {
    console.log(`REST server started at http://localhost:${port}`);
});
