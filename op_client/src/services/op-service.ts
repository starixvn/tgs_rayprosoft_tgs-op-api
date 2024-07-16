import { AxiosInstance } from 'axios';
import FormData from 'form-data';
import fs from 'fs';
import path from 'path';

export default class OperatorService {
    // #region Constructors (1)

    constructor(private http: AxiosInstance) { }

    // #endregion Constructors (1)

    // #region Public Methods (65)

    public checkPhoneCanUse(request: StartSetPhoneRequest): Promise<SuccessResponse<boolean>> {
        return this.http
            .post("/api/player/phone/check", request)
            .then(res => res.data);
    }

    public async createIdentityAuthenticationForm(
        request: CreateIdentityAuthenticationFormRequest,
        filePath: string): Promise<void> {
        let fileName = path.basename(filePath);
        let stream = fs.readFileSync(filePath);

        let formData = new FormData();
        formData.append(
            "request",
            JSON.stringify(request),
            {
                "contentType": "application/json"
            });

        formData.append("file", stream, { filename: fileName });

        await this.http
            .post('/api/player/identity/upload', formData, {
                headers: {
                    ...formData.getHeaders(),
                    'Content-Length': formData.getLengthSync()
                }
            });
    }

    public createWithdraw(request: CreateWithdrawRequest): Promise<string> {
        return this.http
            .post("/api/withdraw", request)
            .then(res => res.data);
    }

    public getAllTypeBucketAmounts(): Promise<Iterable<TypeBucketResult>> {
        return this.http
            .get("/api/player/bucket/type/amount")
            .then(res => res.data);
    }

    public getArticle(id: string): Promise<SuccessResponse<Article>> {
        return this.http
            .get(`/api/article/${id}`)
            .then(res => res.data);
    }

    public getDeliveryLetterContent(deliveryId: string): Promise<SuccessResponse<DeliveryLetter>> {
        return this.http
            .get(`/api/delivery-box/${deliveryId}`)
            .then(res => res.data);
    }

    public getMaintainGameProviders(): Promise<SuccessResponse<GameProviderStatus[]>> {
        return this.http
            .get("/api/gameprovider/maintain")
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

    public getPlayerStationLetterContent(letterId: string): Promise<SuccessResponse<StationLetter | null>> {
        return this.http
            .get(`/api/station-letter/${letterId}`)
            .then(res => res.data);
    }

    public getPlayerStationLetters(onlyUnread: boolean, page: number, rows: number)
        : Promise<SuccessResponse<PagingResponse<StationLetterListItem>>> {
        return this.http
            .get(
                '/api/station-letter',
                {
                    params: {
                        unread: onlyUnread ? true : undefined,
                        page,
                        rows
                    }
                })
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

    public getUBPayExchangeRate(brandPaymentId: string): Promise<GetUBPayExchangeRateResponse> {
        return this.http
            .get(`/api/deposit/crypto/ubpay/exchangerate/${brandPaymentId}`)
            .then(res => res.data);
    }

    public getWithdrawBrandPayments(): Promise<BrandPaymentViewModel[]> {
        return this.http
            .get("/api/withdraw")
            .then(res => res.data);
    }

    public getWithdrawForms(
        request: GetWithdrawFormsRequest,
        paging: PagingRequest): Promise<PagingResponse<WithdrawFormViewModel>> {
        return this.http
            .get("/api/withdraw/player/list", {
                params: {
                    ...request,
                    ...paging
                }
            })
            .then(res => res.data);
    }

    public getWithdrawRequireBucketAmount(): Promise<number> {
        return this.http
            .get("/api/player/bucket/amount")
            .then(res => res.data);
    }

    public getWithdrawRequireBucketAmountOld(): Promise<number> {
        return this.http
            .get("/api/withdraw/player/amount")
            .then(res => res.data);
    }

    public listArticles(take: number | undefined = undefined): Promise<SuccessResponse<ArticleFormListItem>> {
        return this.http
            .get(
                '/api/article',
                {
                    params: {
                        take
                    }
                })
            .then(res => res.data);
    }

    public listBrandSocialMediaSettings(): Promise<SuccessResponse<SocialMediaSetting[]>> {
        return this.http
            .get(`/api/brand/social`)
            .then(res => res.data);
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

    public registerPlayer<TInput>(stepData: PlayerRegisterStep<TInput>): Promise<SuccessResponse<PlayerRegisterStepResponse>> {
        return this.http
            .post("/api/player/register", stepData)
            .then(res => res.data);
    }

    public replyStationLetter(letterId: string, request: ReplyStationLetterRequest): Promise<void> {
        return this.http
            .put(`/api/station-letter/${letterId}:reply`, request)
            .then(res => res.data);
    }

    public sendStationLetterToCustomService(request: SendStationLetterRequest): Promise<void> {
        return this.http
            .put('/api/station-letter', request)
            .then(res => res.data);
    }

    public setPlayerZalo(request: SetZaloRequest): Promise<void> {
        return this.http
            .patch(`/api/player/zalo`, request)
            .then(res => res.data);
    }

    public setStationLetterHaveRead(letterId: string): Promise<void> {
        return this.http
            .patch(`/api/station-letter/${letterId}:read`)
            .then(res => res.data);
    }

    public submit123ThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/123pay", request)
            .then(res => res.data);
    }

    public submitAlfaThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/alfapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitDepositRequest(
        paymentCode: string,
        bankType: string,
        request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post(`/api/deposit/${bankType}/${paymentCode}`, request)
            .then(res => res.data);
    }

    public submitDibaoBankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/dibaopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitDibaoMomoDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/momo/dibaopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitDibaoThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/dibaopay", request)
            .then(res => res.data);
    }

    public submitDibaoZaloDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/zalo/dibaopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitEzThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/ezpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitFeibaoBankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/feibaopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitFeibaoMomoDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/momo/feibaopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitFeibaoThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/feibaopay", request)
            .then(res => res.data);
    }

    public submitFeibaoZaloDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/zalo/feibaopay", request)
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

    public submitFuKaiThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/fukaipay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitGCThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/gcpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitGanyoThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/ganyopay", request)
            .then(res => res.data);
    }

    public submitHThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/hpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitHaloThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/halopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitMalakiV2ThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/malakipay-v2", request)
            .then(res => res.data);
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

    public submitMetaBankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/metapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitMetaThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/metapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitPhpGoThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/phpgopay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitS88BankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/s88pay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitS88ThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/s88pay", request)
            .then(res => res.data);
    }

    public submitSBBankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/sbpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitSBMomoDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/momo/sbpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitSBThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/sbpay", request)
            .then(res => res.data);
    }

    public submitSBZaloDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/zalo/sbpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitShunfaThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/shunfapay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitU2BankDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/bank/u2pay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitU2MomoDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/momo/u2pay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitU2ThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/u2pay", request)
            .then(res => res.data);
    }

    public submitU2ZaloDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/zalo/u2pay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitUBCryptoDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/crypto/ubpay", request)
            .then<SuccessResponse<DepositFormCreatedResponse>>(res => res.data);
    }

    public submitWinThirdPartyDepositRequest(request: DepositRequest): Promise<SuccessResponse<DepositFormCreatedResponse>> {
        return this.http
            .post("/api/deposit/thirdparty/winpay", request)
            .then(res => res.data);
    }

    // #endregion Public Methods (65)
}