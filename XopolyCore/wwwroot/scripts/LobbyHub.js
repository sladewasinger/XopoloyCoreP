"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var signalR = require("@aspnet/signalr");
var LobbyHub = /** @class */ (function () {
    function LobbyHub() {
    }
    LobbyHub.prototype.initialize = function () {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/lobbyhub")
            .build();
        connection.start().catch(function (err) { return document.write(err); });
        connection.on("test", this.test);
    };
    LobbyHub.prototype.test = function (data) {
        console.log("Received Data: ", data);
    };
    return LobbyHub;
}());
exports.LobbyHub = LobbyHub;
//# sourceMappingURL=LobbyHub.js.map