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

    public submitManualDepositUsdt(amount: number, paymentid: string, promotionid: string | null, banktype: string) : Promise<SuccessResponse<string>> {
        return this.http.put(
            "/api/deposit/manual/usdt",
            {   amount, paymentid, promotionid, banktype })
            .then<SuccessResponse<string>>(res => res.data);
    }
}