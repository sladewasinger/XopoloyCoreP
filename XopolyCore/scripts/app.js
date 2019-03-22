"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var LobbyHub_1 = require("./LobbyHub");
var MyApp = /** @class */ (function () {
    function MyApp() {
    }
    MyApp.startSignalR = function () {
        var lobbyHub = new LobbyHub_1.LobbyHub();
        lobbyHub.initialize();
    };
    return MyApp;
}());
exports.MyApp = MyApp;
$(window).on("load", MyApp.startSignalR);
//# sourceMappingURL=app.js.map