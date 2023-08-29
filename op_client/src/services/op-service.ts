import { AxiosInstance } from 'axios';
import fs from 'fs';
import FormData from 'form-data';
import path from 'path';

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

    public submitFiveSevenThirdPartyDepositV2Request(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/fivesevenpay-v2", request)
            .then(res => res.data);
    }

    public getPlayerStationLetters(page: number, rows: number): Promise<SuccessResponse<PagingResponse<StationLetterListItem>>> {
        return this.http
            .get(
                '/api/station-letter',
                {
                    params: {
                        page,
                        rows
                    }
                })
            .then(res => res.data);
    }

    public getPlayerStationLetterContent(letterId: string): Promise<SuccessResponse<StationLetter | null>> {
        return this.http
            .get(`/api/station-letter/${letterId}`)
            .then(res => res.data);
    }

    public sendStationLetterToCustomService(request: SendStationLetterRequest): Promise<void> {
        return this.http
            .put('/api/station-letter', request)
            .then(res => res.data);
    }

    public replyStationLetter(letterId: string, request: ReplyStationLetterRequest): Promise<void> {
        return this.http
            .put(`/api/station-letter/${letterId}:reply`, request)
            .then(res => res.data);
    }

    public setStationLetterHaveRead(letterId: string): Promise<void> {
        return this.http
            .patch(`/api/station-letter/${letterId}:read`)
            .then(res => res.data);
    }

    public getPlayerDeliverLetters(page: number, rows: number): Promise<SuccessResponse<PagingResponse<DeliveryLetterListItem>>> {
        return this.http
            .get(
                '/api/delivery-box',
                {
                    params: {
                        page,
                        rows
                    }
                })
            .then(res => res.data);
    }

    public getDeliveryLetterContent(deliveryId: string): Promise<SuccessResponse<DeliveryLetter>> {
        return this.http
            .get(`/api/delivery-box/${deliveryId}`)
            .then(res => res.data);
    }

    public submitMetaThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/metapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitMetaBankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/metapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitGCThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/gcpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitManualDepositCoinsphRequest(
        amount: number,
        paymentid: string,
        promotionid: string | null,
        banktype: string,        
        name: string | null,
        phone: string | null,
        address: string | null): Promise<SuccessResponse<string>> {
        return this.http
            .put(
                "/api/deposit/manual/coinsph",
                {
                    amount,
                    paymentid,
                    promotionid,
                    banktype,                    
                    name,
                    phone,
                    address
                })
            .then<SuccessResponse<string>>(res => res.data);
    }

    public submitWinThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/winpay", request)
            .then(res => res.data);
    }

    public async createIdentityAuthenticationForm(
        request: CreateIdentityAuthenticationFormRequest, 
        filePath: string): Promise<void>{

        let fileName = path.basename(filePath);
        let stream = fs.readFileSync(filePath);           
          
        let formData = new FormData();
        formData.append(
            "request", 
            JSON.stringify(request),
            {
                "contentType": "application/json"
            });

        formData.append("file",stream, { filename: fileName});
        
        await this.http
            .post('/api/player/identity/upload', formData,{
                headers:{
                    ...formData.getHeaders(),
                    'Content-Length': formData.getLengthSync()
                }
            });            
    }

    public getWithdrawRequireBucketAmount(): Promise<number> {
        return this.http
            .get("/api/withdraw/player/amount")
            .then(res => res.data);
    }
}