import { AxiosInstance } from 'axios';

export default class OperatorService {
    constructor(private http: AxiosInstance) { }

    public getMaintainGameProviders(): Promise<SuccessResponse<GameProviderStatus[]>> {
        return this.http
            .get("/api/gameprovider/maintain")
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

    public submitManualDepositUsdt(
        amount: number,
        paymentid: string,
        promotionid: string | null,
        banktype: string): Promise<SuccessResponse<string>> {
        return this.http.put(
            "/api/deposit/manual/usdt",
            {
                amount,
                paymentid,
                promotionid,
                banktype
            })
            .then<SuccessResponse<string>>(res => res.data);
    }

    public submitManualDepositBankRequest(
        amount: number,
        paymentid: string,
        promotionid: string | null,
        banktype: string,
        account: string,
        name: string | null,
        note: string | null,
        realname: string | null): Promise<SuccessResponse<string>> {
        return this.http
            .put(
                "/api/deposit/manual/bank",
                {
                    amount,
                    paymentid,
                    promotionid,
                    banktype,
                    account,
                    name,
                    note,
                    realname
                })
            .then<SuccessResponse<string>>(res => res.data);
    }

    public submitManualDepositEWalletRequest(
        amount: number,
        paymentid: string,
        promotionid: string | null,
        banktype: string,
        note: string | null,
        emoneyname: string | null,
        phone: string | null): Promise<SuccessResponse<string>> {
        return this.http
            .put(
                "/api/deposit/manual/ewallet",
                {
                    amount,
                    paymentid,
                    promotionid,
                    banktype,
                    note,
                    emoneyname,
                    phone
                })
            .then<SuccessResponse<string>>(res => res.data);
    }

    public submitManualDepositPulsaRequest(
        amount: number,
        paymentid: string,
        promotionid: string | null,
        banktype: string,
        note: string | null,
        realname: string | null,
        phone: string | null): Promise<SuccessResponse<string>> {
        return this.http
            .put(
                "/api/deposit/manual/pulsa",
                {
                    amount,
                    paymentid,
                    promotionid,
                    banktype,
                    note,
                    realname,
                    phone
                })
            .then<SuccessResponse<string>>(res => res.data);
    }

    public playerLogin(username: string, password: string, platformtype: number): Promise<LoginResponse> {
        return this.http
            .post(
                "/api/player/login",
                {
                    username,
                    password,
                    platformtype
                })
            .then<LoginResponse>(res => res.data);
    }

    public submitPhpGoThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/phpgopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitFiveSevenThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/fivesevenpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitMetaThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/metapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }
}