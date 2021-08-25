const path = require("path");
const webpack = require("webpack");

module.exports = {
    mode: 'production',
    resolve: {
        extensions: [".ts", ".js"]
    },
    devtool: "inline-source-map",
    module: {
        rules: [
            {
                test: /\.ts?$/,
                loader: "ts-loader"
            }
        ]
    },
    entry: {
        "webusb": "./src/InitializeWebUSB.ts"
    },
    output: {
        path: path.join(__dirname, "/dist"),
        filename: "[name].js"
    }
};