import { AxiosHttpClient } from './axios-http-client';
import { AxiosInstance } from 'axios';
import * as signalR from '@microsoft/signalr';

export default class NotifyService {
    private connection: signalR.HubConnection;

    constructor(http: AxiosInstance) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(
                "http://localhost/api/notify",
                {
                    httpClient: new AxiosHttpClient(http)
                })
            .configureLogging(signalR.LogLevel.Information)
            .build();
    }

    public async start(): Promise<void> {
        await this.connection.start();
    }
}