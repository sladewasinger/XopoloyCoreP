import { LobbyHub } from "./LobbyHub"

export class MyApp {
    static startSignalR() {
        let lobbyHub = new LobbyHub();
        lobbyHub.initialize();
    }
}


$(window).on("load", MyApp.startSignalR);
