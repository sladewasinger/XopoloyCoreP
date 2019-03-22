import * as signalR from "@aspnet/signalr";

export class LobbyHub {
    public initialize(): void {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/lobbyhub")
            .build();

        connection.start().catch((err:any) => document.write(err));

        connection.on("test", this.test);
    }

    private test(data: any) {
        console.log("Received Data: ", data);
    }
}