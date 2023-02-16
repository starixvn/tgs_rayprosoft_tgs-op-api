interface LoginResponse {
    err: number;
    errmsg: string | undefined | null;
    errmsdid: string | undefined | null;
    token: string | null;
    logintime: number | null;
}