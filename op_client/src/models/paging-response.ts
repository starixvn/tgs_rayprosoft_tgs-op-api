interface PagingResponse<T> {
    totalCount: number;
    items: T[]
}