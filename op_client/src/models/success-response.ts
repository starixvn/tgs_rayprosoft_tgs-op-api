interface SuccessResponse<T> {
    err: number;
    errmsg: string | undefined | null;
    errmsdid: string | undefined | null;
    data: T | null;
}