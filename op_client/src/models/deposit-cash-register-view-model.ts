interface DepositCashRegisterViewModel<T> {
    formid: string;
    amount: number;
    expired?: Date;
    payinfo: T| null;
}