import express from "express";
import compression from "compression";
import bodyParser from "body-parser";
import * as solar from "../index.mjs";

const app = express();
app.use(express.json());
app.use(compression());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.raw({ type: "application/octect-stream" }));

// Register custom endpoints
solar.register(app, "../dist/csv");

app.listen(80, () => {
    console.log("REST server started at port 8081");
});
