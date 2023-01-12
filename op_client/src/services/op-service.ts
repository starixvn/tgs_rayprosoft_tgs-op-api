import { AxiosInstance } from 'axios';

export default class OperatorService {
    constructor(private http: AxiosInstance) { }

    public getMaintainGameProviders(): Promise<SuccessResponse<GameProviderStatus[]>> {
        return this.http.get("/api/gameprovider/maintain")
            .then(res => res.data);
    }

    public getPlayerTransactionLogs(from: Date, to: Date, page: number | null, rows: number | null): Promise<PagingResponse<TransactionLog>> {
        return this.http.get(
            "/api/transaction",
            {
                params: {
                    from,
                    to,
                    page,
                    rows
                }
            })
            .then<PagingResponse<TransactionLog>>(res => res.data);
    }
}