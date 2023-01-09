import { AxiosInstance } from 'axios';
import { GameProviderStatus } from '../models/game-provider-status.js';

export default class OperatorService {
    constructor(private http: AxiosInstance) { }

    public getMaintainGameProviders(): Promise<GameProviderStatus[]> {
        return this.http.get("/api/gameprovider/maintain")
            .then(res => res.data);
    }
}